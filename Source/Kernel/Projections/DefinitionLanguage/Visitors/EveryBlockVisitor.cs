// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Projections.DefinitionLanguage.AST;
using Cratis.Chronicle.Projections.DefinitionLanguage.Parsers;

namespace Cratis.Chronicle.Projections.DefinitionLanguage.Visitors;

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

        context.Advance(); // Skip 'every'

        if (context.Expect(TokenType.Indent) is null) return null;

        var mappings = new List<MappingOperation>();
        var excludeChildren = false;
        var autoMap = false;

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
        return new EveryBlock(mappings, excludeChildren, autoMap);
    }
}
