// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Monads;

namespace Cratis.Chronicle.Projections.DefinitionLanguage.AST;

/// <summary>
/// Represents the root of a projection DSL document.
/// </summary>
/// <param name="Projections">Collection of projections defined in the document.</param>
public record Document(IReadOnlyList<ProjectionNode> Projections) : AstNode
{
    /// <summary>
    /// Validates the document. If the document is invalid, an exception is thrown.
    /// </summary>
    /// <returns>Result indicating success or containing a compiler error.</returns>
    public Result<CompilerError> Validate()
    {
        if (Projections.Count == 0)
        {
            return new CompilerError("Document must contain at least one projection", 0, 0);
        }
        return Result.Success<CompilerError>();
    }
}
