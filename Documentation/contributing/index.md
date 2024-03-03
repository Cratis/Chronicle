# Contributing

Before you start contributing, you should read our guidelines [here](https://github.com/cratis/.github/blob/main/contributing.md).

## Prerequisites

This repository requires the following:

- [Docker](https://www.docker.com/products/docker-desktop)
- [.NET Core 6](https://dotnet.microsoft.com/download/dotnet/6.0)
- [Node JS version 16](https://nodejs.org/)

## Development principles

### Don't fall into the "not invented here" pitfall

### Focus on maintainability of the code

### Code should be tested and made testable

### Favor characteristics over principles (CUPID over SOLID)

## Code Tour

This project has a set of [code tours](https://marketplace.visualstudio.com/items?itemName=vsls-contrib.codetour) that guide you through
the project structure and where to find what. Install the [VSCode extension](https://marketplace.visualstudio.com/items?itemName=vsls-contrib.codetour)
and open the **Code Tour explorer** for different tours.

## Build and Test

All C# based projects are added to the solution file at the root level, you can therefor
build it quite easily from root:

```shell
dotnet build
```

Similarly with the specifications you can do:

```shell
dotnet test
```

If you're using an IDE such as Visual Studio, Rider or similar - open the [cratis.sln](../../cratis.sln)
file and do the build / test run from within the IDE.

> Note: if it takes a long time on every build, perform a `dotnet clean` from the project you're working on
> and then `dotnet build` - subsequent builds should then be fast. This is due to the Orleans CodeGenerator being confused
> and hindering incremental builds.

## Running locally

## Database

Cratis is built using MongoDB as a storage engine. It leverages some features that require MongoDB be in cluster mode.
For convenience running locally for development there is an [image](https://hub.docker.com/r/cratis/mongodb)
specifically set up with the features needed.

```shell
docker run -p 27017:27017 cratis/mongodb
```

## Backend

The Cratis Kernel can be started by navigating your terminal to [/Source/Kernel/Server](../../Source/Kernel/Server)
and run `dotnet run`.

If you're interested debugging through, there are a set VSCode launch configuration that can be used.
Open the debug panel in VSCode and select the profile you want and click the debug button or hit F5.

![](./debug.gif)

Running the server alone is the **.NET Core Launch (Server) profile**, while the **Server and Bank Sample** is a debug compound
of the server and the bank sample.

### Frontend

For the Kernel you can run the Workbench on top by navigating to the [./Source/Workbench](../../Source/Workbench) and
then run `yarn dev`. You can then point your browser to [http://localhost:9000](http://localhost:9000).

## Static Code Analysis

All projects are built using the same static code analysis rules found at the root of the repository in a file called `.globalconfig`.
