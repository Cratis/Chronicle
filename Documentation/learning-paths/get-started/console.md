# Console

Start by creating a folder for your project and then create a .NET console project inside this folder.

> Note: If you're using an IDE or editor that lets you do this without using the terminal, that is fine as well.

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
