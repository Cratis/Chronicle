// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Captures.Engine.DeclarationLanguage.AST;
using Cratis.Chronicle.Captures.Engine.DeclarationLanguage.Visitors;

namespace Cratis.Chronicle.Captures.Engine.DeclarationLanguage.Parsers;

/// <summary>
/// Parses nested blocks.
/// </summary>
public class NestedBlockParser
{
    readonly AppendBlockParser _appendBlocks = new();
    readonly MapBlockParser _mapBlocks = new();

    /// <summary>
    /// Parses a nested block from the given context.
    /// </summary>
    /// <param name="context">The parsing context.</param>
    /// <returns>The parsed nested block, or null if parsing failed.</returns>
    public NestedBlock? Parse(IParsingContext context)
    {
        var nestedToken = context.Expect(TokenType.Nested);
        if (nestedToken is null)
        {
            return null;
        }

        var objectPath = ParsingHelpers.ParsePropertyPath(context);
        if (objectPath is null)
        {
            return null;
        }

        if (context.Expect(TokenType.Indent, "Expected an indented nested block") is null)
        {
            return null;
        }

        MapBlock? map = null;
        var appends = new List<AppendBlock>();
        while (!context.Check(TokenType.Dedent) && !context.IsAtEnd)
        {
            if (context.Check(TokenType.Map))
            {
                map = _mapBlocks.Parse(context);
                continue;
            }

            if (context.Check(TokenType.Append))
            {
                var append = _appendBlocks.Parse(context);
                if (append is not null)
                {
                    appends.Add(append);
                }
                continue;
            }

            context.ReportError($"Unexpected token '{context.Current.Value}' in nested block");
            context.Advance();
        }

        context.Expect(TokenType.Dedent);

        return new NestedBlock(objectPath, map, appends)
        {
            Line = nestedToken.Line,
            Column = nestedToken.Column
        };
    }
}
