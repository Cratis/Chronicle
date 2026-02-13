// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Projections.DefinitionLanguage.AST;

/// <summary>
/// Base class for all AST nodes.
/// </summary>
public abstract record AstNode
{
    /// <summary>
    /// Gets the line number where this node appears in the source (1-based).
    /// </summary>
    public int Line { get; init; } = 1;

    /// <summary>
    /// Gets the column number where this node appears in the source (1-based).
    /// </summary>
    public int Column { get; init; } = 1;
}
