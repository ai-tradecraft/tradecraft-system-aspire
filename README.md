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

Set `LAMPLIGHTER_OPENCODE_USE_REAL_BACKEND=1` when the local OpenCode
configuration should be used as the live model backend.
