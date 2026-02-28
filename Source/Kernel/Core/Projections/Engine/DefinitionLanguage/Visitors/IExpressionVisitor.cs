// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Projections.Engine.DefinitionLanguage.AST;

namespace Cratis.Chronicle.Projections.Engine.DefinitionLanguage.Visitors;

/// <summary>
/// Defines a visitor for parsing expressions from tokens.
/// </summary>
public interface IExpressionVisitor
{
    /// <summary>
    /// Parses an expression from the current token position.
    /// </summary>
    /// <param name="context">The parsing context.</param>
    /// <returns>The parsed expression, or null if parsing failed.</returns>
    Expression? Visit(IParsingContext context);
}
