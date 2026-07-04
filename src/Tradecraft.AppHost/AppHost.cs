var builder = DistributedApplication.CreateBuilder(args);

var orchestratorApi = builder
    .AddProject<Projects.ChatThroughHarness_Api>("orchestrator-api")
    .WithHttpsEndpoint()
    .WithExternalHttpEndpoints();

var lamplighterRepository = Path.GetFullPath(
    "../../submodules/lamplighter-opencode",
    builder.AppHostDirectory);
var contractsRepository = Path.GetFullPath(
    "../../submodules/tradecraft-contracts",
    builder.AppHostDirectory);
var adapterDeploymentMode = Environment.GetEnvironmentVariable("TRADECRAFT_RUNTIME_ADAPTER_DEPLOYMENT_MODE");

var lamplighterController = builder
    .AddProject<Projects.Lamplighter_Controller>("lamplighter-controller")
    .WithReference(orchestratorApi)
    .WithEnvironment(
        "Runner__OrchestratorBaseUri",
        orchestratorApi.GetEndpoint("https"))
    .WithEnvironment("Runner__Adapter__WorkingDirectory", lamplighterRepository)
    .WithEnvironment("TRADECRAFT_CONTRACTS_ROOT", contractsRepository)
    .WaitFor(orchestratorApi);

if (string.Equals(adapterDeploymentMode, "local_container", StringComparison.Ordinal))
{
    var controllerWorkspace = Environment.GetEnvironmentVariable("LAMPLIGHTER_CONTROLLER_WORKSPACE");
    if (string.IsNullOrWhiteSpace(controllerWorkspace))
    {
        controllerWorkspace = Path.GetFullPath(
            "../../.tradecraft/lamplighter-controller-workspace",
            builder.AppHostDirectory);
    }

    Directory.CreateDirectory(controllerWorkspace);
    var image = Environment.GetEnvironmentVariable("LAMPLIGHTER_OPENCODE_CONTAINER_IMAGE")
        ?? "lamplighter-opencode:local";
    var containerRuntime = Environment.GetEnvironmentVariable("CONTAINER_RUNTIME")
        ?? "docker";
    var dockerArguments = new[]
    {
        "run",
        "--rm",
        "-e", $"TRADECRAFT_CONTRACTS_ROOT={contractsRepository}",
        "-e", "LAMPLIGHTER_ADAPTER_DEPLOYMENT_MODE=local_container",
        "-v", $"{controllerWorkspace}:{controllerWorkspace}",
        "-v", $"{contractsRepository}:{contractsRepository}:ro",
        image
    };

    lamplighterController
        .WithEnvironment("LAMPLIGHTER_CONTROLLER_WORKSPACE", controllerWorkspace)
        .WithEnvironment("Runner__Adapter__Kind", "opencode-container")
        .WithEnvironment("Runner__Adapter__Executable", containerRuntime)
        .WithEnvironment("Runner__Adapter__WorkingDirectory", lamplighterRepository);

    for (var index = 0; index < dockerArguments.Length; index++)
    {
        lamplighterController.WithEnvironment($"Runner__Adapter__ArgumentPrefix__{index}", dockerArguments[index]);
    }
}

foreach (var name in new[]
{
    "LAMPLIGHTER_OPENCODE_USE_REAL_BACKEND",
    "OPENCODE_DISABLE_AUTOUPDATE",
})
{
    var value = Environment.GetEnvironmentVariable(name);
    if (!string.IsNullOrWhiteSpace(value))
    {
        lamplighterController.WithEnvironment(name, value);
    }
}

builder
    .AddViteApp(
        "operator-portal",
        "../../submodules/lamplighter-opencode/poc/chat-through-harness/client")
    .WithNpm()
    // The stable Vite integration manages an HTTP development endpoint. Its
    // HTTPS certificate helper is still experimental in Aspire 13.4.
    .WithEnvironment("BROWSER", "none")
    .WithReference(orchestratorApi)
    .WithEnvironment(
        "VITE_API_BASE_URL",
        orchestratorApi.GetEndpoint("https"))
    .WaitFor(orchestratorApi)
    .WithExternalHttpEndpoints();

var sqliteConnection = builder.AddParameter(
    "asset-storage-sqlite-connection",
    "Data Source=asset-storage.db");

builder
    .AddProject<Projects.AssetStorage_WebHost>("asset-storage")
    .WithEnvironment(
        "AssetStorage__Sqlite__ConnectionString",
        sqliteConnection)
    .WithHttpHealthCheck("/health")
    .WithExternalHttpEndpoints();

await builder.Build().RunAsync().ConfigureAwait(false);
