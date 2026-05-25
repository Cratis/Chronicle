// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Captures.Engine.DeclarationLanguage.AST;
using Cratis.Chronicle.Captures.Engine.DeclarationLanguage.Visitors;

namespace Cratis.Chronicle.Captures.Engine.DeclarationLanguage.Parsers;

/// <summary>
/// Parses capture nodes.
/// </summary>
public class CaptureParser
{
    readonly CaptureDirectiveParser _directives = new();

    /// <summary>
    /// Parses a capture from the given context.
    /// </summary>
    /// <param name="context">The parsing context.</param>
    /// <returns>The parsed capture node, or null if parsing failed.</returns>
    public CaptureNode? Parse(IParsingContext context)
    {
        if (!context.Check(TokenType.Capture))
        {
            context.ReportError("Expected 'capture'");
            return null;
        }

        var captureToken = context.Current;
        context.Advance();

        var name = ParsingHelpers.ParsePropertyPath(context);
        if (name is null)
        {
            return null;
        }

        var directives = new List<CaptureDirective>();
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
                    context.Advance();
                }
            }

            context.Expect(TokenType.Dedent);
        }

        return new CaptureNode(name, directives)
        {
            Line = captureToken.Line,
            Column = captureToken.Column
        };
    }
}
