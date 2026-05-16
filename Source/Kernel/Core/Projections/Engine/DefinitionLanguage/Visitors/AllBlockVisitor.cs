// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Projections.Definitions;
using Cratis.Chronicle.Projections.Engine.DeclarationLanguage.AST;
using Cratis.Chronicle.Projections.Engine.DeclarationLanguage.Parsers;

namespace Cratis.Chronicle.Projections.Engine.DeclarationLanguage.Visitors;

/// <summary>
/// Visitor for parsing all blocks.
/// </summary>
public class AllBlockVisitor : IDirectiveVisitor
{
    readonly MappingOperationParser _mappingOperations = new();

    /// <inheritdoc/>
    public ProjectionDirective? Visit(IParsingContext context)
    {
        if (!context.Check(TokenType.All))
        {
            return null;
        }

        var allToken = context.Current;
        context.Advance(); // Skip 'all'

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

            context.Expect(TokenType.Dedent);
        }

        return new AllBlock(mappings, autoMap)
        {
            Line = allToken.Line,
            Column = allToken.Column
        };
    }
}
