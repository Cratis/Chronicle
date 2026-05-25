// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Captures.Engine.DeclarationLanguage.AST;
using Cratis.Chronicle.Captures.Engine.DeclarationLanguage.Visitors;

namespace Cratis.Chronicle.Captures.Engine.DeclarationLanguage.Parsers;

/// <summary>
/// Parses capture directives.
/// </summary>
public class CaptureDirectiveParser
{
    readonly AppendBlockParser _appendBlocks = new();
    readonly ChildrenBlockParser _childrenBlocks = new();
    readonly MapBlockParser _mapBlocks = new();
    readonly NestedBlockParser _nestedBlocks = new();
    readonly SourceBlockParser _sourceBlocks = new();

    /// <summary>
    /// Parses a directive from the given context.
    /// </summary>
    /// <param name="context">The parsing context.</param>
    /// <returns>The parsed directive, or null if parsing failed.</returns>
    public CaptureDirective? Parse(IParsingContext context)
    {
        if (context.Check(TokenType.Source))
        {
            return _sourceBlocks.Parse(context);
        }

        if (context.Check(TokenType.Key))
        {
            var token = context.Current;
            context.Advance();
            var property = ParsingHelpers.CollectLineRemainder(context, token.Line);
            if (string.IsNullOrWhiteSpace(property))
            {
                context.ReportError("Expected key property");
                return null;
            }

            return new KeyDirective(property)
            {
                Line = token.Line,
                Column = token.Column
            };
        }

        if (context.Check(TokenType.Map))
        {
            return _mapBlocks.Parse(context);
        }

        if (context.Check(TokenType.Append))
        {
            return _appendBlocks.Parse(context);
        }

        if (context.Check(TokenType.Nested))
        {
            return _nestedBlocks.Parse(context);
        }

        if (context.Check(TokenType.Children))
        {
            return _childrenBlocks.Parse(context);
        }

        context.ReportError($"Unexpected token '{context.Current.Value}' in capture body");
        return null;
    }
}
