// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Monads;

namespace Cratis.Chronicle.Captures.Engine.DeclarationLanguage.AST;

/// <summary>
/// Represents a single capture definition.
/// </summary>
/// <param name="Name">The capture name.</param>
/// <param name="Directives">The capture directives.</param>
public record CaptureNode(string Name, IReadOnlyList<CaptureDirective> Directives) : AstNode
{
    /// <summary>
    /// Validates the capture.
    /// </summary>
    /// <returns>Result indicating success or containing a compiler error.</returns>
    public Result<CompilerError> Validate()
    {
        if (Directives.Count == 0)
        {
            return new CompilerError($"Capture '{Name}' must contain at least one directive", Line, Column);
        }

        return Result.Success<CompilerError>();
    }
}
