---
applyTo: "**/*.cs"
---

# 🧪 Specific instructions for C#

## Technology Stack

- .NET 9 (specified in global.json)
- C# 13
- ASP.NET Core
- MongoDB.Driver for C#
- Entity Framework Core
- xUnit for testing
- NSubstitute for mocking
- Cratis.Specifications for BDD-style tests

## Building

- Don't use the build tasks in Visual Studio. Use `dotnet build` from the command line instead.
- Use `dotnet format` to format code.
- Use `dotnet test` to run tests.
