// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Projections.Definitions;
using Cratis.Chronicle.Projections.DefinitionLanguage.AST;
using Cratis.Chronicle.Projections.DefinitionLanguage.Parsers;

namespace Cratis.Chronicle.Projections.DefinitionLanguage.Visitors;

/// <summary>
/// Visitor for parsing join blocks.
/// </summary>
public class JoinBlockVisitor : IDirectiveVisitor
{
    readonly TypeRefParser _typeRefs = new();
    readonly MappingOperationParser _mappingOperations = new();

    /// <inheritdoc/>
    public ProjectionDirective? Visit(IParsingContext context)
    {
        if (!context.Check(TokenType.Join))
        {
            return null;
        }

        var joinToken = context.Current;
        context.Advance(); // Skip 'join'

        var joinNameToken = context.Expect(TokenType.Identifier);
        if (joinNameToken is null) return null;
        var joinName = joinNameToken.Value;

        if (context.Expect(TokenType.On) is null) return null;

        var onPropertyToken = context.Expect(TokenType.Identifier);
        if (onPropertyToken is null) return null;
        var onProperty = onPropertyToken.Value;

        // After "on property", expect newline then indent
        if (context.Check(TokenType.NewLine)) context.Advance();
        if (context.Expect(TokenType.Indent) is null) return null;

        var withBlocks = new List<WithEventBlock>();

        // Parse all 'with' event blocks
        while (!context.Check(TokenType.Dedent) && !context.IsAtEnd)
        {
            if (context.Check(TokenType.With))
            {
                var withBlock = ParseWithEventBlock(context);
                if (withBlock is not null)
                {
                    withBlocks.Add(withBlock);
                }
            }
            else
            {
                context.Advance(); // Skip unexpected tokens
            }
        }

        context.Expect(TokenType.Dedent);
        return new JoinBlock(joinName, onProperty, withBlocks)
        {
            Line = joinToken.Line,
            Column = joinToken.Column
        };
    }

    WithEventBlock? ParseWithEventBlock(IParsingContext context)
    {
        if (!context.Check(TokenType.With))
        {
            return null;
        }

        context.Advance(); // Skip 'with'

        var eventType = _typeRefs.Parse(context);
        if (eventType is null) return null;

        if (context.Check(TokenType.NewLine)) context.Advance();
        if (!context.Check(TokenType.Indent))
        {
            // Empty with block - no mappings, just the event type
            return new WithEventBlock(eventType, AutoMap.Inherit, []);
        }

        context.Advance(); // Skip indent

        var autoMap = AutoMap.Inherit;
        var mappings = new List<MappingOperation>();

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
        return new WithEventBlock(eventType, autoMap, mappings);
    }
}
