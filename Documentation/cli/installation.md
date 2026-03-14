# Installation

The Cratis CLI is distributed as a .NET global tool via NuGet.

## Prerequisites

- .NET SDK 10.0 or later

## Install

```shell
dotnet tool install --global Cratis.Chronicle.Cli
```

The package installs a global command called `cratis`.

## Update

The CLI includes a built-in update command:

```shell
cratis update
```

To update to a specific version:

```shell
cratis update --version 2.0.0
```

Alternatively, use `dotnet tool update` directly:

```shell
dotnet tool update --global Cratis.Chronicle.Cli
```

## Version and Compatibility

Check the installed CLI version and its compatibility with the connected server:

```shell
cratis version
```

The CLI also checks for updates in the background. When a newer version is available on NuGet, a hint appears after command output.

## Verify

```shell
cratis --version
```

## Uninstall

```shell
dotnet tool uninstall --global Cratis.Chronicle.Cli
```

## What's Next

See [Configuration](./configuration.md) to connect the CLI to your Chronicle server.
