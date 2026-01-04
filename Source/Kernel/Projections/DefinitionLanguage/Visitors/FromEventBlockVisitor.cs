// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Projections.Definitions;
using Cratis.Chronicle.Projections.DefinitionLanguage.AST;
using Cratis.Chronicle.Projections.DefinitionLanguage.Parsers;

namespace Cratis.Chronicle.Projections.DefinitionLanguage.Visitors;

/// <summary>
/// Visitor for parsing from-event blocks.
/// </summary>
public class FromEventBlockVisitor : IDirectiveVisitor
{
    readonly TypeRefParser _typeRefs = new();
    readonly ExpressionParser _expressions = new();
    readonly MappingOperationParser _mappingOperations = new();
    readonly KeyDirectiveParser _keyDirectives = new();

    /// <inheritdoc/>
    public ProjectionDirective? Visit(IParsingContext context)
    {
        if (!context.Check(TokenType.From))
        {
            return null;
        }

        context.Advance(); // Skip 'from'

        // Parse multiple comma-separated events on same line
        var eventSpecs = new List<(TypeRef EventType, Expression? Key)>();

        while (!context.IsAtEnd)
        {
            var eventType = _typeRefs.Parse(context);
            if (eventType is null) return null;

            Expression? key = null;

            // Check for inline key for this specific event
            if (context.Check(TokenType.Key))
            {
                context.Advance();
                key = _expressions.Parse(context);
                if (key is null) return null;
            }

            eventSpecs.Add((eventType, key));

            // Check if there's a comma for another event
            if (context.Check(TokenType.Comma))
            {
                context.Advance(); // Skip comma

                // Continue loop to parse next event
            }
            else
            {
                // No more events, exit loop
                break;
            }
        }

        // Skip newline if present after all events
        if (context.Check(TokenType.NewLine)) context.Advance();

        // Check for shared inline options that apply to all events
        var autoMap = AutoMap.Inherit; // Default to inherit (no explicit directive)
        while (!context.Check(TokenType.Indent) && !context.IsAtEnd)
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
                break;
            }
        }

        // Only process mappings if there's an indent (i.e., there's a body)
        var mappings = new List<MappingOperation>();
        Expression? parentKey = null;
        Expression? blockLevelKey = null;

        if (!context.Check(TokenType.Indent))
        {
            // No body for from blocks - consume newline and create blocks with inline options only
            if (context.Check(TokenType.NewLine)) context.Advance();

            if (eventSpecs.Count == 1)
            {
                var singleEvent = eventSpecs[0];
                return new FromEventBlock(
                    singleEvent.EventType,
                    autoMap,
                    singleEvent.Key,
                    null,
                    null,
                    []);
            }

            return new MultiFromEventBlock(
                eventSpecs.ConvertAll(e => new FromEventBlock(
                    e.EventType,
                    autoMap,
                    e.Key,
                    null,
                    null,
                    [])));
        }

        if (context.Expect(TokenType.Indent) is null) return null;
        CompositeKeyDirective? compositeKey = null;

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
            else if (context.Check(TokenType.Parent))
            {
                context.Advance();
                parentKey = _expressions.Parse(context);
            }
            else if (context.Check(TokenType.Key))
            {
                if (blockLevelKey is not null || compositeKey is not null)
                {
                    context.ReportError("Duplicate key directive. A key has already been defined for this event block.");
                    context.Advance();
                    continue;
                }

                var keyDirective = _keyDirectives.Parse(context);
                if (keyDirective is CompositeKeyDirective ck)
                {
                    compositeKey = ck;
                }
                else if (keyDirective is KeyDirective kd)
                {
                    blockLevelKey = kd.Expression;
                }
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

        // Create a FromEventBlock for each event spec
        // Since we can only return one, we need a MultiFromEventBlock wrapper
        if (eventSpecs.Count == 1)
        {
            var (eventType, inlineKey) = eventSpecs[0];
            var finalKey = inlineKey ?? (compositeKey is null && blockLevelKey is null ? null : blockLevelKey);
            return new FromEventBlock(eventType, autoMap, finalKey, compositeKey, parentKey, mappings);
        }

        // Multiple events - create a MultiFromEventBlock
        var blocks = eventSpecs.ConvertAll(spec =>
        {
            var (eventType, inlineKey) = spec;

            // Use inline key if provided, otherwise fall back to block-level key/compositeKey
            var finalKey = inlineKey ?? (compositeKey is null && blockLevelKey is null ? null : blockLevelKey);
            return new FromEventBlock(eventType, autoMap, finalKey, compositeKey, parentKey, mappings);
        });

        return new MultiFromEventBlock(blocks);
    }
}
