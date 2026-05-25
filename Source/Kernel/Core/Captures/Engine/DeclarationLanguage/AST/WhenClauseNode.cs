// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Captures;

namespace Cratis.Chronicle.Captures.Engine.DeclarationLanguage.AST;

/// <summary>
/// Represents a parsed when clause.
/// </summary>
/// <param name="Type">The clause type.</param>
/// <param name="Properties">Properties referenced by the clause.</param>
/// <param name="FromValue">Optional source value.</param>
/// <param name="ToValue">Optional target value.</param>
/// <param name="Expression">Optional raw expression.</param>
public record WhenClauseNode(
    WhenClauseType Type,
    IReadOnlyList<string> Properties,
    string? FromValue = default,
    string? ToValue = default,
    string? Expression = default) : AstNode;
