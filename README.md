# Tradecraft System Aspire

This repository owns the Aspire composition for the Tradecraft system. It keeps
the runnable system host and public/service submodules out of the private
`tradecraft-meta` documentation repository.

## Repository layout

- `src/Tradecraft.AppHost` — Aspire AppHost for the current system slice.
- `submodules/` — component repositories composed by the AppHost:
  - `asset-storage`
  - `asset-storage-client-py`
  - `lamplighter-controller`
  - `lamplighter-opencode`
  - `tradecraft-contracts`

## Getting started

From this repository root:

```sh
git submodule update --init --recursive
dotnet restore Tradecraft.System.slnx
dotnet build Tradecraft.System.slnx
aspire start --apphost src/Tradecraft.AppHost/Tradecraft.AppHost.csproj
```

The AppHost forwards these environment variables to the Lamplighter controller
when they are present:

- `LAMPLIGHTER_OPENCODE_USE_REAL_BACKEND`
- `OPENCODE_DISABLE_AUTOUPDATE`
- `TRADECRAFT_RUNTIME_ADAPTER_DEPLOYMENT_MODE`
- `LAMPLIGHTER_OPENCODE_CONTAINER_IMAGE`
- `CONTAINER_RUNTIME`

Set `LAMPLIGHTER_OPENCODE_USE_REAL_BACKEND=1` when the local OpenCode
configuration should be used as the live model backend.

Set `TRADECRAFT_RUNTIME_ADAPTER_DEPLOYMENT_MODE=local_container` to have the
controller invoke the OpenCode adapter through Docker/Podman while preserving
the same `adapter.operation` contract. Build and verify the adapter image first:

```sh
make -C submodules/lamplighter-opencode test-container-adapter-operation
```
