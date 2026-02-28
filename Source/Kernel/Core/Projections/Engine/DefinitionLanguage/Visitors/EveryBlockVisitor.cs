// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Projections.Definitions;
using Cratis.Chronicle.Projections.Engine.DefinitionLanguage.AST;
using Cratis.Chronicle.Projections.Engine.DefinitionLanguage.Parsers;

namespace Cratis.Chronicle.Projections.Engine.DefinitionLanguage.Visitors;

/// <summary>
/// Visitor for parsing every blocks.
/// </summary>
public class EveryBlockVisitor : IDirectiveVisitor
{
    readonly MappingOperationParser _mappingOperations = new();

    /// <inheritdoc/>
    public ProjectionDirective? Visit(IParsingContext context)
    {
        if (!context.Check(TokenType.Every))
        {
            return null;
        }

        var everyToken = context.Current;
        context.Advance(); // Skip 'every'

        if (context.Expect(TokenType.Indent) is null) return null;

        var mappings = new List<MappingOperation>();
        var excludeChildren = false;
        var autoMap = AutoMap.Inherit; // Default to inherit (no explicit directive)

        while (!context.Check(TokenType.Dedent) && !context.IsAtEnd)
        {
            if (context.Check(TokenType.Exclude))
            {
                context.Advance();
                context.Expect(TokenType.Children);
                excludeChildren = true;
            }
            else if (context.Check(TokenType.AutoMap))
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
        return new EveryBlock(mappings, excludeChildren, autoMap)
        {
            Line = everyToken.Line,
            Column = everyToken.Column
        };
    }
}
