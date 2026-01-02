// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Projections.LanguageDefinition.AST;

/// <summary>
/// Represents the root of a projection DSL document.
/// </summary>
/// <param name="Projections">Collection of projections defined in the document.</param>
public record Document(IReadOnlyList<ProjectionNode> Projections) : AstNode
{
    /// <summary>
    /// Validates the document. If the document is invalid, an exception is thrown.
    /// </summary>
    /// <exception cref="DocumentMustHaveAtLeastOneProjection">Thrown when the document does not contain any projections.</exception>
    public void Validate()
    {
        if (Projections.Count == 0)
        {
            throw new DocumentMustHaveAtLeastOneProjection();
        }
    }
}
