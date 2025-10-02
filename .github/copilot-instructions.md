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
- Prefer using `record` types for immutable data structures.
- Use expression-bodied members for simple methods and properties.
- Use `async` and `await` for asynchronous programming.
- Use `Task` and `Task<T>` for asynchronous methods.
- Use `IEnumerable<T>` for collections that are not modified.
- Never return mutable collections from public APIs.
- Don't use regions in the code.
- Never add postfixes like Async, Impl, etc. to class or method names.
- Favor collection initializers and object initializers.
- Use string interpolation instead of string.Format or concatenation.
- Favor primary constructors for all types.

## Exceptions

- Use exceptions for exceptional situations only.
- Don't use exceptions for control flow.
- Always provide a meaningful message when throwing an exception.
- Always create a custom exception type that derives from Exception.
- Never use any built-in exception types like InvalidOperationException, ArgumentException, etc.
- Add XML documentation for exceptions being thrown.
- XML documentation for exception should start with "The exception that is thrown when ...".

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
- Use the `[LoggerMessage]` attribute to define log messages.
- Don't include `eventId` in the `[LoggerMessage]` attribute.

## Dependency Injection

- Systems that have a convention of IFoo to Foo does not need to be registered explicitly.
- Prefer constructor injection over method injection.
- Avoid service locator pattern (i.e., avoid using IServiceProvider directly).
- For implementations that should be singletons, use the `[Singleton]` attribute on the class

## Testing

- Follow the following guides:
   - [How to Write Specs](./instructions/specs.instructions.md)
   - [How to Write Entity Framework Core Specs](./instructions/efcore.specs.instructions.md)
   - [Concepts](./instructions/concepts.instructions.md)
   - [Documentation](./instructions/documentation.instructions.md)
   - [Pull Requests](./instructions/pull-requests.instructions.md)

## Header

All files should start with the following header:

```csharp
// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
```

