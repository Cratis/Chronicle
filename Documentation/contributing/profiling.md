# Profiling

[Dotnet-trace](https://docs.microsoft.com/en-us/dotnet/core/diagnostics/dotnet-trace) offers a simple way to collect profiling
telemetry. This can then be used with [Speedscope](https://www.speedscope.app) by importing the result.

## Client

The simplest way to profile the client would be to go via a Sample application, e.g. the Bank sample.
Navigate to [/Samples/Banking/Bank/Main](../../Samples/Banking/Bank/Main) and run the following after a build.

```shell
dotnet trace collect --format Speedscope -- dotnet bin/Debug/net6.0/Main.dll
```

> Note: You'll see a file called **dotnet_<date_time>.speedscope.json** file, this is the one you'll upload to Speedscope.

## Kernel

Similar for the Kernel you can simply go to [/Source/Kernel/Server](../../Source/Kernel/Server) folder and run:

```shell
dotnet trace collect --format Speedscope -- dotnet bin/Debug/net6.0/Aksio.Cratis.Kernel.Server.dll
```

> Note: You'll see a file called **dotnet_<date_time>.speedscope.json** file, this is the one you'll upload to Speedscope.
