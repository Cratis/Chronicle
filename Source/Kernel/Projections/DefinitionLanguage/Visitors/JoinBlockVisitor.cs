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
        if (context.Expect(TokenType.Events) is null) return null;

        var firstEventType = _typeRefs.Parse(context);
        if (firstEventType is null) return null;

        var eventTypes = new List<TypeRef> { firstEventType };
        while (context.Check(TokenType.Comma))
        {
            context.Advance();
            var eventType = _typeRefs.Parse(context);
            if (eventType is not null)
            {
                eventTypes.Add(eventType);
            }
        }

        var autoMap = AutoMap.Inherit; // Default to inherit (no explicit directive)
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
        return new JoinBlock(joinName, onProperty, eventTypes, autoMap, mappings)
        {
            Line = joinToken.Line,
            Column = joinToken.Column
        };
    }
}
