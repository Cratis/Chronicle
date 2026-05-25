// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Captures.Engine.DeclarationLanguage.AST;
using Cratis.Chronicle.Captures.Engine.DeclarationLanguage.Visitors;

namespace Cratis.Chronicle.Captures.Engine.DeclarationLanguage.Parsers;

/// <summary>
/// Parses map blocks.
/// </summary>
public class MapBlockParser
{
    /// <summary>
    /// Parses a map block from the given context.
    /// </summary>
    /// <param name="context">The parsing context.</param>
    /// <returns>The parsed map block, or null if parsing failed.</returns>
    public MapBlock? Parse(IParsingContext context)
    {
        var mapToken = context.Expect(TokenType.Map);
        if (mapToken is null)
        {
            return null;
        }

        if (context.Expect(TokenType.Indent, "Expected an indented map block") is null)
        {
            return null;
        }

        var operations = new List<MapOperationNode>();
        while (!context.Check(TokenType.Dedent) && !context.IsAtEnd)
        {
            var operation = ParseOperation(context);
            if (operation is not null)
            {
                operations.Add(operation);
            }
            else
            {
                context.Advance();
            }
        }

        context.Expect(TokenType.Dedent);

        return new MapBlock(operations)
        {
            Line = mapToken.Line,
            Column = mapToken.Column
        };
    }

    MapOperationNode? ParseOperation(IParsingContext context)
    {
        if (context.Check(TokenType.Split))
        {
            return ParseSplit(context);
        }

        if (!context.Check(TokenType.Identifier))
        {
            context.ReportError("Expected mapping operation");
            return null;
        }

        var targetToken = context.Current;
        context.Advance();
        if (context.Expect(TokenType.Equals) is null)
        {
            return null;
        }

        var line = targetToken.Line;
        var source = ParsingHelpers.CollectUntil(context, line, TokenType.Translate);
        if (string.IsNullOrWhiteSpace(source))
        {
            context.ReportError("Expected mapping value");
            return null;
        }

        if (context.Check(TokenType.Translate) && context.Current.Line == line)
        {
            context.Advance();
            return ParseTranslate(context, targetToken, source);
        }

        if (source.StartsWith('`') && source.EndsWith('`'))
        {
            return new TemplateAssignNode(targetToken.Value, source[1..^1])
            {
                Line = targetToken.Line,
                Column = targetToken.Column
            };
        }

        return new FieldRenameNode(targetToken.Value, source)
        {
            Line = targetToken.Line,
            Column = targetToken.Column
        };
    }

    TranslateNode? ParseTranslate(IParsingContext context, Token targetToken, string source)
    {
        if (context.Expect(TokenType.Indent, "Expected an indented translate block") is null)
        {
            return null;
        }

        var entries = new List<TranslateEntryNode>();
        while (!context.Check(TokenType.Dedent) && !context.IsAtEnd)
        {
            var from = ParsingHelpers.ParseSimpleValue(context);
            if (from is null)
            {
                return null;
            }

            if (context.Expect(TokenType.Arrow) is null)
            {
                return null;
            }

            var to = ParsingHelpers.ParseSimpleValue(context);
            if (to is null)
            {
                return null;
            }

            entries.Add(new TranslateEntryNode(from, to)
            {
                Line = targetToken.Line,
                Column = targetToken.Column
            });
        }

        context.Expect(TokenType.Dedent);

        return new TranslateNode(targetToken.Value, source, entries)
        {
            Line = targetToken.Line,
            Column = targetToken.Column
        };
    }

    SplitNode? ParseSplit(IParsingContext context)
    {
        var splitToken = context.Expect(TokenType.Split);
        if (splitToken is null)
        {
            return null;
        }

        var source = ParsingHelpers.ParsePropertyPath(context);
        if (source is null)
        {
            return null;
        }

        if (context.Expect(TokenType.By) is null)
        {
            return null;
        }

        var separatorToken = context.Expect(TokenType.StringLiteral, "Expected split separator string");
        if (separatorToken is null)
        {
            return null;
        }

        if (context.Expect(TokenType.Indent, "Expected an indented split block") is null)
        {
            return null;
        }

        var targets = new List<string>();
        while (!context.Check(TokenType.Dedent) && !context.IsAtEnd)
        {
            var target = ParsingHelpers.ParsePropertyPath(context);
            if (target is not null)
            {
                targets.Add(target);
            }
        }

        context.Expect(TokenType.Dedent);

        return new SplitNode(source, separatorToken.Value, targets)
        {
            Line = splitToken.Line,
            Column = splitToken.Column
        };
    }
}
