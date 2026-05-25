// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Monads;

namespace Cratis.Chronicle.Captures.Engine.DeclarationLanguage.AST;

/// <summary>
/// Represents the root of a capture definition document.
/// </summary>
/// <param name="Captures">Collection of captures defined in the document.</param>
public record CaptureDocument(IReadOnlyList<CaptureNode> Captures) : AstNode
{
    /// <summary>
    /// Validates the document.
    /// </summary>
    /// <returns>Result indicating success or containing a compiler error.</returns>
    public Result<CompilerError> Validate()
    {
        if (Captures.Count == 0)
        {
            return new CompilerError("Document must contain at least one capture", 0, 0);
        }

        return Result.Success<CompilerError>();
    }
}
