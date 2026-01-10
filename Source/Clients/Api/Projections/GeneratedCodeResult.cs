// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Api.Projections;

/// <summary>
/// Represents the result of generating C# code from DSL.
/// </summary>
/// <param name="Code">The generated C# code.</param>
/// <param name="Errors">Any syntax errors encountered during generation.</param>
public record GeneratedCodeResult(string Code, IEnumerable<ProjectionDefinitionSyntaxError> Errors);
