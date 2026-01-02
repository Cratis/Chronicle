// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

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

        var eventType = _typeRefs.Parse(context);
        if (eventType is null) return null;

        var autoMap = false;
        Expression? key = null;

        // Check for inline options
        while (!context.Check(TokenType.Indent) && !context.IsAtEnd)
        {
            if (context.Check(TokenType.AutoMap))
            {
                context.Advance();
                autoMap = true;
            }
            else if (context.Check(TokenType.Key))
            {
                context.Advance();
                key = _expressions.Parse(context);
            }
            else
            {
                break;
            }
        }

        if (context.Expect(TokenType.Indent) is null) return null;

        var mappings = new List<MappingOperation>();
        Expression? parentKey = null;
        CompositeKeyDirective? compositeKey = null;

        while (!context.Check(TokenType.Dedent) && !context.IsAtEnd)
        {
            if (context.Check(TokenType.AutoMap))
            {
                context.Advance();
                autoMap = true;
            }
            else if (context.Check(TokenType.Parent))
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
        return new FromEventBlock(eventType, autoMap, key, compositeKey, parentKey, mappings);
    }
}
