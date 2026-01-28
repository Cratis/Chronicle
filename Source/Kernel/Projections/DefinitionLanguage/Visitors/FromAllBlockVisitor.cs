// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Projections.Definitions;
using Cratis.Chronicle.Projections.DefinitionLanguage.AST;
using Cratis.Chronicle.Projections.DefinitionLanguage.Parsers;

namespace Cratis.Chronicle.Projections.DefinitionLanguage.Visitors;

/// <summary>
/// Visitor for parsing from $all blocks.
/// </summary>
public class FromAllBlockVisitor : IDirectiveVisitor
{
    readonly MappingOperationParser _mappingOperations = new();

    /// <inheritdoc/>
    public ProjectionDirective? Visit(IParsingContext context)
    {
        if (!context.Check(TokenType.From))
        {
            return null;
        }

        var fromToken = context.Current;

        // Peek ahead to check if this is a $all expression
        var nextToken = context.Peek();
        if (nextToken.Type != TokenType.Dollar)
        {
            return null;
        }

        // Peek one more to verify it's 'all'
        var thirdToken = context.Peek(2);
        if (thirdToken.Type != TokenType.Identifier || !thirdToken.Value.Equals("all", StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        // Now we know it's a from $all, so we can consume tokens
        context.Advance(); // Skip 'from'
        context.Advance(); // Skip '$'
        context.Advance(); // Skip 'all'

        // Skip newline if present
        if (context.Check(TokenType.NewLine)) context.Advance();

        var mappings = new List<MappingOperation>();
        var autoMap = AutoMap.Inherit;

        // Process block body if present
        if (context.Check(TokenType.Indent))
        {
            context.Advance(); // Skip indent

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
                    var mapping = _mappingOperations.Parse(context);
                    if (mapping is not null)
                    {
                        mappings.Add(mapping);
                    }
                    else
                    {
                        context.Advance();
                    }
                }
            }

            if (context.Check(TokenType.Dedent))
            {
                context.Advance();
            }
        }

        return new FromAllBlock(mappings, autoMap)
        {
            Line = fromToken.Line,
            Column = fromToken.Column
        };
    }
}
