# GitHub Copilot Instructions

## Technology Stack

- .NET 9 (specified in global.json)
- C# 13
- ASP.NET Core
- MongoDB.Driver for C#
- Entity Framework Core
- xUnit for testing
- NSubstitute for mocking
- Cratis.Specifications for BDD-style tests

## General

- Make only high confidence suggestions when reviewing code changes.
- Always use the latest version C#, currently C# 13 features.
- Never change global.json unless explicitly asked to.
- Never change package.json or package-lock.json files unless explicitly asked to.
- Never change NuGet.config files unless explicitly asked to.
- Never leave unused using statements in the code.
- Always ensure that the code compiles without warnings.
- Always ensure that the code passes all tests.
- Always ensure that the code adheres to the project's coding standards.
- Always ensure that the code is maintainable.

## Formatting

- Honor the existing code style and conventions in the project.
- Apply code-formatting style defined in .editorconfig.
- Prefer file-scoped namespace declarations and single-line using directives.
- Insert a new line before the opening curly brace of any code block (e.g., after `if`, `for`, `while`, `foreach`, `using`, `try`, etc.).
- Ensure that the final return statement of a method is on its own line.
- Use pattern matching and switch expressions wherever possible.
- Use `nameof` instead of string literals when referring to member names.
- Place private class declarations at the bottom of the file.

## Instructions

- Write clear and concise comments for each function.
- Prefer `var` over explicit types when declaring variables.
- Do not add unnecessary comments or documentation.
- Use `using` directives for namespaces at the top of the file.
- Sort the `using` directives alphabetically.
- Use namespaces that match the folder structure.
- Remove unused `using` directives.
- Use file-scoped namespace declarations.
- Use single-line using directives.
- For types that does not have an implementation, don't add a body (e.g., `public interface IMyInterface;`).

## Nullable Reference Types

- Always use is null or is not null instead of == null or != null.
- Trust the C# null annotations and don't add null checks when the type system says a value cannot be null.

## Naming Conventions

- Follow PascalCase for component names, method names, and public members.
- Use camelCase for private fields and local variables.
- Prefix private fields with an underscore (e.g., `_privateField`).
- Prefix interface names with "I" (e.g., IUserService).

## Logging

- Use structured logging with named parameters.
- Use appropriate log levels (Information, Warning, Error, Debug).
- Always use a generic ILogger<T> where T is the class name.
- Keep logging in separate partial methods for better readability. Call the file <SystemName>Logging.cs. Make this class partial and static and internal and all methods should be internal.

## Dependency Injection

- Systems that have a convention of IFoo to Foo does not need to be registered explicitly.
- Prefer constructor injection over method injection.
- Avoid service locator pattern (i.e., avoid using IServiceProvider directly).
- For implementations that should be singletons, use the `[Singleton]` attribute on the class

## Testing

- Follow the following guides:
   - [How to Write Specs](./instructions/specs.instructions.md)
   - [How to Write Entity Framework Core Specs](./instructions/efcore.specs.instructions.md)

## Header

All files should start with the following header:

```csharp
// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
```
