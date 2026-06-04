// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Projections.Definitions;
using Cratis.Chronicle.Projections.Engine.DeclarationLanguage.AST;

namespace Cratis.Chronicle.Projections.Engine.DeclarationLanguage.Visitors;

/// <summary>
/// Visitor for parsing nested object blocks that appear inside a children or another nested block.
/// </summary>
internal sealed class NestedChildBlockVisitor
{
    /// <summary>
    /// Visits and parses a nested block in a child context.
    /// </summary>
    /// <param name="context">The parsing context.</param>
    /// <returns>The parsed child block, or null if not applicable.</returns>
    public ChildBlock? Visit(IParsingContext context)
    {
        if (!context.Check(TokenType.Nested))
        {
            return null;
        }

        var nestedToken = context.Current;
        context.Advance(); // Skip 'nested'

        var propertyNameToken = context.Expect(TokenType.Identifier);
        if (propertyNameToken is null) return null;
        var propertyName = propertyNameToken.Value;

        if (context.Expect(TokenType.Indent) is null) return null;

        var autoMap = AutoMap.Inherit;
        var childBlocks = new List<ChildBlock>();
        var hasFrom = false;

        while (!context.Check(TokenType.Dedent) && !context.IsAtEnd)
        {
            if (context.Check(TokenType.AutoMap))
            {
                context.Advance();
                autoMap = AutoMap.Enabled;
            }
            else if (context.Check(TokenType.No) && context.Peek().Type == TokenType.AutoMap)
            {
                context.Advance(); // Skip 'no'
                context.Advance(); // Skip 'automap'
                autoMap = AutoMap.Disabled;
            }
            else
            {
                var childBlock = NestedBlockParsing.ParseChildBlock(context);
                if (childBlock is not null)
                {
                    if (childBlock is ChildOnEventBlock)
                    {
                        hasFrom = true;
                    }

                    childBlocks.Add(childBlock);
                }
            }
        }

        context.Expect(TokenType.Dedent);

        if (!hasFrom)
        {
            context.Errors.Add(
                $"Nested block '{propertyName}' must contain at least one 'from' directive",
                nestedToken.Line,
                nestedToken.Column);
        }

        return new NestedChildBlock(propertyName, autoMap, childBlocks)
        {
            Line = nestedToken.Line,
            Column = nestedToken.Column
        };
    }
}
