// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Projections.Definitions;
using Cratis.Chronicle.Projections.DefinitionLanguage.AST;
using Cratis.Chronicle.Projections.DefinitionLanguage.Parsers;

namespace Cratis.Chronicle.Projections.DefinitionLanguage.Visitors;

/// <summary>
/// Visitor for parsing children blocks.
/// </summary>
public class ChildrenBlockVisitor : IDirectiveVisitor
{
    readonly ExpressionParser _expressions = new();

    /// <inheritdoc/>
    public ProjectionDirective? Visit(IParsingContext context)
    {
        if (!context.Check(TokenType.Children))
        {
            return null;
        }

        var childrenToken = context.Current;
        context.Advance(); // Skip 'children'

        var collectionNameToken = context.Expect(TokenType.Identifier);
        if (collectionNameToken is null) return null;
        var collectionName = collectionNameToken.Value;

        if (context.Expect(TokenType.Identified) is null) return null;
        if (context.Expect(TokenType.By) is null) return null;

        var identifierExpr = _expressions.Parse(context);
        if (identifierExpr is null) return null;

        if (context.Expect(TokenType.Indent) is null) return null;

        var autoMap = AutoMap.Inherit; // Default to inherit (no explicit directive)
        var childBlocks = new List<ChildBlock>();

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
                var childBlock = ParseChildBlock(context);
                if (childBlock is not null)
                {
                    childBlocks.Add(childBlock);
                }
            }
        }

        context.Expect(TokenType.Dedent);
        return new ChildrenBlock(collectionName, identifierExpr, autoMap, childBlocks)
        {
            Line = childrenToken.Line,
            Column = childrenToken.Column
        };
    }

    ChildBlock? ParseChildBlock(IParsingContext context)
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

        if (context.Check(TokenType.Remove))
        {
            var visitor = new RemoveBlockVisitor();
            return visitor.Visit(context);
        }

        if (context.Check(TokenType.Every))
        {
            var visitor = new ChildEveryBlockVisitor();
            return visitor.Visit(context);
        }

        context.ReportError($"Unexpected token '{context.Current.Value}' in children block");
        return null;
    }
}
