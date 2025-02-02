# Quickstart Console

[!INCLUDE [pre-requisites](./prereq.md)]

## Objective

In this quickstart, you will create a simple solution that covers the most important aspects of getting started with Chronicle.
The sample will focus on a straightforward and well-understood domain: a library.

You can find the complete working sample [here](https://github.com/Cratis/Samples/tree/main/Chronicle/Quickstart/Console).
which also leverages common things from [here](https://github.com/Cratis/Samples/tree/main/Chronicle/Quickstart/Common).

[!INCLUDE [docker](./docker.md)]

## Setup project

Start by creating a folder for your project and then create a .NET console project inside this folder:

```shell
dotnet new console
```

Add a reference to the [Chronicle client package](https://www.nuget.org/packages/Cratis.Chronicle):

```shell
dotnet add package Cratis.Chronicle
```

## Client

Chronicle is accessed through its client called `ChronicleClient`.
From this instance, you can get the event store you want to work with.

The simplest approach is to rely on the automatic discovery of artifacts by instructing the event store to
discover and register everything automatically.

The following snippet configures the minimum and discovers everything for you:

{{snippet:Quickstart-Console-Setup}}

[!INCLUDE [common](./common.md)]

[!INCLUDE [common](./mongodb.md)]

With this you can query the collections as expected using the **MongoDB.Driver**:

{{snippet:Quickstart-Books}}
