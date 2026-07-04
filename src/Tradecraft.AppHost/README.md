# Tradecraft System AppHost

This Aspire AppHost is the local composition root for the Tradecraft system. It
starts and observes system processes; it does not replace the durable Agent
Runtime Orchestrator or the Lamplighter Controller.

## Run

From the meta-repository root:

```sh
aspire run --isolated --apphost src/Tradecraft.AppHost
```

Aspire prints an authenticated dashboard URL. Keep the command attached while
using the system, and press `Ctrl+C` to stop this AppHost.

`aspire start --isolated` is not the recommended local command with Aspire CLI
13.4.6: the resources can reach `Running`, but the detached isolated host may
exit after its CLI backchannel disconnects. Attached `aspire run --isolated`
keeps the composition alive and supports `aspire describe`, `aspire logs`, and
`aspire wait`.

## Resources

| Resource | Purpose | Dependencies |
| --- | --- | --- |
| `orchestrator-api` | Current Agent Runtime Orchestrator precursor and operator API. | None |
| `lamplighter-controller` | Outbound controller that polls the orchestrator and manages local agent runtimes. | Waits for `orchestrator-api`. |
| `operator-portal` | React/Vite operator UI. | Installs npm dependencies and waits for `orchestrator-api`. |
| `asset-storage` | Independent system asset API. | Local SQLite connection parameter. |

The AppHost injects allocated endpoint references rather than fixed URLs:

- `Runner__OrchestratorBaseUri` points the controller at the API.
- `VITE_API_BASE_URL` points the portal at the API.
- `Runner__Adapter__WorkingDirectory` identifies the configured runtime adapter
  checkout.
- `AssetStorage__Sqlite__ConnectionString` comes from the
  `asset-storage-sqlite-connection` AppHost parameter.

Provider credentials and other secrets are inherited from the controller
process environment for this POC. They are not embedded in the AppHost source.

## Adapter Deployment Modes

The default AppHost path uses the local-process OpenCode adapter:

```sh
uv run lamplighter-opencode adapter-operation --operation operation.json --json
```

To prove the same controller protocol against the local-container deployment
mode, first build/verify the adapter image:

```sh
make -C submodules/lamplighter-opencode test-container-adapter-operation
```

Then start the AppHost with:

```sh
export TRADECRAFT_RUNTIME_ADAPTER_DEPLOYMENT_MODE=local_container
export LAMPLIGHTER_OPENCODE_CONTAINER_IMAGE=lamplighter-opencode:local
aspire run --isolated --apphost src/Tradecraft.AppHost
```

This switches only `Runner__Adapter__Executable` and
`Runner__Adapter__ArgumentPrefix__*` so the Lamplighter controller invokes:

```sh
docker run --rm ... lamplighter-opencode:local adapter-operation --operation operation.json --json
```

The controller-to-adapter contract remains the same `adapter.operation` /
`adapter.operation_result` envelope. The AppHost mounts the controller
workspace and `tradecraft-contracts` checkout so result refs and schema
ownership stay outside the image. Set `CONTAINER_RUNTIME=podman` to use Podman.

## Inspect

```sh
aspire describe --apphost src/Tradecraft.AppHost --format Json
aspire wait orchestrator-api --apphost src/Tradecraft.AppHost
aspire wait lamplighter-controller --apphost src/Tradecraft.AppHost
aspire wait operator-portal --apphost src/Tradecraft.AppHost
aspire wait asset-storage --apphost src/Tradecraft.AppHost
aspire logs lamplighter-controller --apphost src/Tradecraft.AppHost
```

For the demo checkpoint, use the full runbook:

```sh
python3 scripts/aspire-demo-readiness.py
python3 scripts/aspire-demo-agent-turn.py --expect-fake
```

See [Aspire Demo Runbook](../../.agent-docs/aspire-demo-runbook.md) for the
operator walkthrough and troubleshooting notes.

For live OpenCode mode, keep this AppHost as the single orchestration root,
configure OpenCode directly for the provider/model you want, and start Aspire
from a shell that has passed:

```sh
export LAMPLIGHTER_OPENCODE_USE_REAL_BACKEND=1
python3 scripts/aspire-live-opencode-preflight.py
```

Then verify the live path with:

```sh
python3 scripts/aspire-demo-agent-turn.py --expect-live --timeout-seconds 300
```

The Vite development endpoint uses HTTP. The orchestrator API uses HTTPS with
the trusted ASP.NET Core development certificate. Aspire's JavaScript
development-certificate helper remains experimental in 13.4.6, so the AppHost
does not suppress its stability diagnostic.

The AppHost forwards only the Lamplighter fake/live selector and neutral
OpenCode process controls. The Lamplighter controller scrubs provider/model,
OpenCode config path/content, and provider API-key environment variables before
invoking the runtime adapter; OpenCode itself owns provider/model/auth
configuration.

## Current Boundary

Asset Storage is composed and observable but is not yet consumed by the POC
orchestrator. A dependency will be added when the orchestrator implements a
real content or artifact storage use case. Service Defaults for the POC API and
controller, durable orchestrator stores, identity, policy, and production
deployment manifests remain later milestones.
