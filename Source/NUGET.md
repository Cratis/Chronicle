# Cratis Chronicle

Cratis Chronicle is an open-source (MIT) event-sourcing database and processing runtime with an Orleans-based
kernel, pluggable storage (MongoDB default; PostgreSQL, SQL Server, SQLite, in-memory), and language-agnostic
gRPC contracts.

Chronicle stores immutable events and provides projections, reactors, reducers, constraints and concurrency
controls for building event-driven applications.

## Installation

For ASP.NET Core applications:

```shell
dotnet add package Cratis.Chronicle.AspNetCore
```

For console applications and worker services:

```shell
dotnet add package Cratis.Chronicle
```

To host Chronicle with .NET Aspire:

```shell
dotnet add package Cratis.Chronicle.Aspire
```

Integration-testing support is available in
[Cratis.Chronicle.XUnit.Integration](https://www.nuget.org/packages/Cratis.Chronicle.XUnit.Integration/).
See the [getting-started guides](https://www.cratis.io/chronicle/get-started/) for kernel hosting and client setup.

## Documentation

- [Chronicle documentation](https://www.cratis.io/chronicle/)
- [Concepts](https://www.cratis.io/chronicle/concepts/)
- [Architecture](https://www.cratis.io/chronicle/architecture/)
- [Source code and examples](https://github.com/Cratis/Chronicle)

## Support and contributions

- [Report a bug or request a feature](https://github.com/Cratis/Chronicle/issues)
- [Join the community on Discord](https://discord.gg/kt4AMpV8WV)
- [Contributing guide](https://github.com/Cratis/Chronicle/blob/main/Documentation/contributing/index.md)
- [MIT license](https://github.com/Cratis/Chronicle/blob/main/LICENSE)
