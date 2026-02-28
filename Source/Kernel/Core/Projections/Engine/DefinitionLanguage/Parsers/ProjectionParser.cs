// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Projections.Engine.DefinitionLanguage.AST;
using Cratis.Chronicle.Projections.Engine.DefinitionLanguage.Visitors;

namespace Cratis.Chronicle.Projections.Engine.DefinitionLanguage.Parsers;

/// <summary>
/// Parses projection nodes.
/// </summary>
internal sealed class ProjectionParser
{
    static readonly TokenType[] _directiveKeywords =
    [
        TokenType.From,
        TokenType.Every,
        TokenType.Key,
        TokenType.Parent,
        TokenType.On,
        TokenType.With,
        TokenType.Join,
        TokenType.Children,
        TokenType.Remove,
        TokenType.Increment,
        TokenType.Decrement,
        TokenType.Add,
        TokenType.Subtract,
        TokenType.Count,
        TokenType.Sequence,
        TokenType.No,
        TokenType.AutoMap
    ];

    readonly TypeRefParser _typeRefs = new();
    readonly ProjectionDirectiveParser _directives = new();

    /// <summary>
    /// Parses a projection from the given context.
    /// </summary>
    /// <param name="context">The parsing context.</param>
    /// <returns>The parsed projection node, or null if parsing failed.</returns>
    public ProjectionNode? Parse(IParsingContext context)
    {
        if (!context.Check(TokenType.Projection))
        {
            // Check if this is a directive keyword that should be indented
            if (IsDirectiveKeyword(context.Current.Type))
            {
                context.ReportError($"Directive '{context.Current.Value}' must be indented inside a projection block");

                // Skip to end of line to recover
                SkipToEndOfLine(context);
                return null;
            }

            context.ReportError("Expected 'projection'");
            return null;
        }
        var projectionToken = context.Current;
        context.Advance();

        var projectionName = _typeRefs.Parse(context);
        if (projectionName is null) return null;

        if (context.Expect(TokenType.Arrow) is null) return null;

        var readModelType = _typeRefs.Parse(context);
        if (readModelType is null) return null;

        var directives = new List<ProjectionDirective>();

        // Allow minimal projections without body (no indent)
        if (context.Check(TokenType.Indent))
        {
            context.Advance();

            while (!context.Check(TokenType.Dedent) && !context.IsAtEnd)
            {
                var directive = _directives.Parse(context);
                if (directive is not null)
                {
                    directives.Add(directive);
                }
                else
                {
                    // No directive matched, advance to avoid infinite loop
                    context.Advance();
                }
            }

            if (context.Check(TokenType.Dedent))
            {
                context.Advance();
            }
        }

        return new ProjectionNode(projectionName.Name, readModelType, directives)
        {
            Line = projectionToken.Line,
            Column = projectionToken.Column
        };
    }

    static bool IsDirectiveKeyword(TokenType type) => _directiveKeywords.Contains(type);

    static void SkipToEndOfLine(IParsingContext context)
    {
        // Skip tokens until we hit end of input or a token on a new line
        var currentLine = context.Current.Line;
        while (!context.IsAtEnd && context.Current.Line == currentLine)
        {
            context.Advance();
        }
    }
}
