// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Projections.Engine.DeclarationLanguage.AST;
using Cratis.Chronicle.Projections.Engine.DeclarationLanguage.Parsers;

namespace Cratis.Chronicle.Projections.Engine.DeclarationLanguage.Visitors;

/// <summary>
/// Shared helpers for parsing the body of a nested object block.
/// </summary>
internal static class NestedBlockParsing
{
    /// <summary>
    /// Parses a single child block that can appear inside a nested block — from, join, nested, clear with, every, or further children.
    /// </summary>
    /// <param name="context">The parsing context.</param>
    /// <returns>The parsed child block, or null if no child block could be parsed.</returns>
    public static ChildBlock? ParseChildBlock(IParsingContext context)
    {
        if (context.Check(TokenType.From))
        {
            var visitor = new ChildOnEventBlockVisitor();
            return visitor.Visit(context);
        }

        if (context.Check(TokenType.Join))
        {
            var visitor = new ChildJoinBlockVisitor();
            return visitor.Visit(context);
        }

        if (context.Check(TokenType.Children))
        {
            var visitor = new NestedChildrenBlockVisitor();
            return visitor.Visit(context);
        }

        if (context.Check(TokenType.Nested))
        {
            var visitor = new NestedChildBlockVisitor();
            return visitor.Visit(context);
        }

        if (context.Check(TokenType.Clear))
        {
            return ParseClearWith(context);
        }

        if (context.Check(TokenType.Every))
        {
            var visitor = new ChildEveryBlockVisitor();
            return visitor.Visit(context);
        }

        if (context.Check(TokenType.Remove))
        {
            var visitor = new RemoveBlockVisitor();
            return visitor.Visit(context);
        }

        context.ReportError($"Unexpected token '{context.Current.Value}' in nested block");
        return null;
    }

    /// <summary>
    /// Parses a 'clear with EventType' directive.
    /// </summary>
    /// <param name="context">The parsing context.</param>
    /// <returns>The parsed clear with directive, or null if parsing failed.</returns>
    public static ClearWithDirective? ParseClearWith(IParsingContext context)
    {
        if (!context.Check(TokenType.Clear))
        {
            return null;
        }

        var clearToken = context.Current;
        context.Advance(); // Skip 'clear'

        if (context.Expect(TokenType.With) is null) return null;

        var typeRefs = new TypeRefParser();
        var eventType = typeRefs.Parse(context);
        if (eventType is null) return null;

        return new ClearWithDirective(eventType)
        {
            Line = clearToken.Line,
            Column = clearToken.Column
        };
    }
}
