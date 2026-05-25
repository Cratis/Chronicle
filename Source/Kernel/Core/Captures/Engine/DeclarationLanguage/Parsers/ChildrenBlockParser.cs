// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Captures.Engine.DeclarationLanguage.AST;
using Cratis.Chronicle.Captures.Engine.DeclarationLanguage.Visitors;

namespace Cratis.Chronicle.Captures.Engine.DeclarationLanguage.Parsers;

/// <summary>
/// Parses children blocks.
/// </summary>
public class ChildrenBlockParser
{
    readonly AppendBlockParser _appendBlocks = new();
    readonly MapBlockParser _mapBlocks = new();

    /// <summary>
    /// Parses a children block from the given context.
    /// </summary>
    /// <param name="context">The parsing context.</param>
    /// <returns>The parsed children block, or null if parsing failed.</returns>
    public ChildrenBlock? Parse(IParsingContext context)
    {
        var childrenToken = context.Expect(TokenType.Children);
        if (childrenToken is null)
        {
            return null;
        }

        var collectionProperty = ParsingHelpers.ParsePropertyPath(context);
        if (collectionProperty is null)
        {
            return null;
        }

        if (context.Expect(TokenType.Identified) is null || context.Expect(TokenType.By) is null)
        {
            return null;
        }

        var identifiedBy = ParsingHelpers.ParsePropertyPath(context);
        if (identifiedBy is null)
        {
            return null;
        }

        if (context.Expect(TokenType.Indent, "Expected an indented children block") is null)
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

            context.ReportError($"Unexpected token '{context.Current.Value}' in children block");
            context.Advance();
        }

        context.Expect(TokenType.Dedent);

        return new ChildrenBlock(collectionProperty, identifiedBy, map, appends)
        {
            Line = childrenToken.Line,
            Column = childrenToken.Column
        };
    }
}
