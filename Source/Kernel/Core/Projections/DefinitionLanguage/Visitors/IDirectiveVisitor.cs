// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Projections.DefinitionLanguage.AST;

namespace Cratis.Chronicle.Projections.DefinitionLanguage.Visitors;

/// <summary>
/// Defines a visitor for parsing projection directives from tokens.
/// </summary>
public interface IDirectiveVisitor
{
    /// <summary>
    /// Attempts to parse a directive from the current token position.
    /// </summary>
    /// <param name="context">The parsing context.</param>
    /// <returns>The parsed directive, or null if this visitor cannot handle the current token.</returns>
    ProjectionDirective? Visit(IParsingContext context);
}
