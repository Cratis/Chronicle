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

```shell
dotnet tool update --global Cratis.Chronicle.Cli
```

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
