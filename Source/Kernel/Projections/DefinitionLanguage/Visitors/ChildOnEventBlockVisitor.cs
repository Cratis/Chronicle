// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Projections.DefinitionLanguage.AST;
using Cratis.Chronicle.Projections.DefinitionLanguage.Parsers;

namespace Cratis.Chronicle.Projections.DefinitionLanguage.Visitors;

/// <summary>
/// Visitor for parsing child on event blocks.
/// </summary>
internal class ChildOnEventBlockVisitor
{
    readonly TypeRefParser _typeRefs = new();
    readonly ExpressionParser _expressions = new();
    readonly MappingOperationParser _mappingOperations = new();
    readonly KeyDirectiveParser _keyDirectives = new();
    /// <summary>
    /// Visits and parses a child on-event block.
    /// </summary>
    /// <param name="context">The parsing context.</param>
    /// <returns>The parsed child block or null if not applicable.</returns>
    public ChildBlock? Visit(IParsingContext context)
    {
        if (!context.Check(TokenType.From))
        {
            return null;
        }

        context.Advance(); // Skip 'from'

        var eventType = _typeRefs.Parse(context);
        if (eventType is null) return null;

        Expression? key = null;

        // Check for inline key
        if (context.Check(TokenType.Key))
        {
            context.Advance();
            key = _expressions.Parse(context);
        }

        // Skip newline if present
        if (context.Check(TokenType.NewLine)) context.Advance();

        // If no indent, this block has no body (everything was inline)
        if (!context.Check(TokenType.Indent))
        {
            return new ChildOnEventBlock(eventType, key, null, null, []);
        }

        if (context.Expect(TokenType.Indent) is null) return null;

        var mappings = new List<MappingOperation>();
        Expression? parentKey = null;
        CompositeKeyDirective? compositeKey = null;

        while (!context.Check(TokenType.Dedent) && !context.IsAtEnd)
        {
            if (context.Check(TokenType.Parent))
            {
                context.Advance();
                parentKey = _expressions.Parse(context);
            }
            else if (context.Check(TokenType.Key))
            {
                var keyDirective = _keyDirectives.Parse(context);
                if (keyDirective is CompositeKeyDirective ck)
                {
                    compositeKey = ck;
                }
                else if (keyDirective is KeyDirective kd)
                {
                    key = kd.Expression;
                }
            }
            else if (context.Check(TokenType.AutoMap))
            {
                // AutoMap is not yet supported in child event blocks, skip it
                context.Advance();
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
        return new ChildOnEventBlock(eventType, key, compositeKey, parentKey, mappings);
    }
}
