// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Monads;

namespace Cratis.Chronicle.Projections.Engine.DeclarationLanguage.AST;

/// <summary>
/// Represents a projection definition.
/// </summary>
/// <param name="Name">The projection name.</param>
/// <param name="ReadModelType">The target read model type. When null, the read model is inferred from event types.</param>
/// <param name="Directives">Collection of projection-level directives and blocks.</param>
public record ProjectionNode(
    string Name,
    TypeRef? ReadModelType,
    IReadOnlyList<ProjectionDirective> Directives) : AstNode
{
    /// <summary>
    /// Gets whether this projection has an explicitly specified read model type.
    /// </summary>
    public bool HasExplicitReadModel => ReadModelType is not null;

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
