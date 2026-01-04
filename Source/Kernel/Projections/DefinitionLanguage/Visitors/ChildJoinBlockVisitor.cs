// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Projections.DefinitionLanguage.AST;
using Cratis.Chronicle.Projections.DefinitionLanguage.Parsers;

namespace Cratis.Chronicle.Projections.DefinitionLanguage.Visitors;

/// <summary>
/// Visitor for parsing child join blocks.
/// </summary>
internal sealed class ChildJoinBlockVisitor
{
    readonly TypeRefParser _typeRefs = new();
    readonly MappingOperationParser _mappingOperations = new();

    /// <summary>
    /// Visits and parses a child join block.
    /// </summary>
    /// <param name="context">The parsing context.</param>
    /// <returns>The parsed child block or null if not applicable.</returns>
    public ChildBlock? Visit(IParsingContext context)
    {
        if (!context.Check(TokenType.Join))
        {
            return null;
        }

        context.Advance(); // Skip 'join'

        var joinNameToken = context.Expect(TokenType.Identifier);
        if (joinNameToken is null) return null;
        var joinName = joinNameToken.Value;

        if (context.Expect(TokenType.On) is null) return null;

        var onPropertyToken = context.Expect(TokenType.Identifier);
        if (onPropertyToken is null) return null;
        var onProperty = onPropertyToken.Value;

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

        var autoMap = false;
        var mappings = new List<MappingOperation>();

        while (!context.Check(TokenType.Dedent) && !context.IsAtEnd)
        {
            if (context.Check(TokenType.AutoMap))
            {
                context.Advance();
                autoMap = true;
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
        return new ChildJoinBlock(joinName, onProperty, eventTypes, autoMap, mappings);
    }
}
