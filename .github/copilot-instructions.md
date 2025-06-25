# GitHub Copilot Instructions

- Prefer `var` over explicit types when declaring variables.
- Use the latest C# version features
- Do not add unnecessary comments or documentation.
- Use `using` directives for namespaces at the top of the file.
- Use namespaces that match the folder structure.
- Use file-scoped namespace declarations.
- Use single-line using directives.
- Honor the existing code style and conventions in the project.
- Honor the .editorconfig file for formatting and style rules.
- Never use `private` access modifier, as C# defaults to `private`.
- Fields and properties should should be prefixed with _ e.g. `_<field name>` using camelCase.

## Testing

- [How to Write Specs](./instructions/tests.md)

## Header

All files should start with the following header:

```csharp
// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
```
