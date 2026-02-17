// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Monads;

namespace Cratis.Chronicle.Projections.Engine.DefinitionLanguage.AST;

/// <summary>
/// Represents a projection definition.
/// </summary>
/// <param name="Name">The projection name.</param>
/// <param name="ReadModelType">The target read model type.</param>
/// <param name="Directives">Collection of projection-level directives and blocks.</param>
public record ProjectionNode(
    string Name,
    TypeRef ReadModelType,
    IReadOnlyList<ProjectionDirective> Directives) : AstNode
{
    /// <summary>
    /// Validates the projection.
    /// </summary>
    /// <returns>Result indicating success or containing a compiler error.</returns>
    public Result<CompilerError> Validate()
    {
        if (Directives.Count == 0)
        {
            return new CompilerError($"Projection '{Name}' must contain at least one directive", 0, 0);
        }

        return Result<CompilerError>.Success();
    }
}
