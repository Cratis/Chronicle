// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Captures.Engine.DeclarationLanguage.AST;
using Cratis.Chronicle.Captures.Engine.DeclarationLanguage.Visitors;

namespace Cratis.Chronicle.Captures.Engine.DeclarationLanguage.Parsers;

/// <summary>
/// Parses append blocks.
/// </summary>
public class AppendBlockParser
{
    readonly WhenClauseParser _whenClauses = new();

    /// <summary>
    /// Parses an append block from the given context.
    /// </summary>
    /// <param name="context">The parsing context.</param>
    /// <returns>The parsed append block, or null if parsing failed.</returns>
    public AppendBlock? Parse(IParsingContext context)
    {
        var appendToken = context.Expect(TokenType.Append);
        if (appendToken is null)
        {
            return null;
        }

        var eventType = ParsingHelpers.ParsePropertyPath(context);
        if (eventType is null)
        {
            return null;
        }

        if (context.Expect(TokenType.Indent, "Expected an indented append block") is null)
        {
            return null;
        }

        WhenClauseNode? when = null;
        var assignments = new List<FieldAssignmentNode>();
        while (!context.Check(TokenType.Dedent) && !context.IsAtEnd)
        {
            if (context.Check(TokenType.When))
            {
                when = _whenClauses.Parse(context);
                continue;
            }

            var assignment = ParseAssignment(context);
            if (assignment is not null)
            {
                assignments.Add(assignment);
            }
            else
            {
                context.Advance();
            }
        }

        context.Expect(TokenType.Dedent);

        if (when is null)
        {
            context.ReportError("Append block must contain a when clause");
            return null;
        }

        return new AppendBlock(eventType, when, assignments)
        {
            Line = appendToken.Line,
            Column = appendToken.Column
        };
    }

    FieldAssignmentNode? ParseAssignment(IParsingContext context)
    {
        if (!context.Check(TokenType.Identifier))
        {
            context.ReportError("Expected field assignment");
            return null;
        }

        var targetToken = context.Current;
        context.Advance();
        if (context.Expect(TokenType.Equals) is null)
        {
            return null;
        }

        var source = ParsingHelpers.CollectLineRemainder(context, targetToken.Line);
        if (string.IsNullOrWhiteSpace(source))
        {
            context.ReportError("Expected assignment source");
            return null;
        }

        return new FieldAssignmentNode(targetToken.Value, source)
        {
            Line = targetToken.Line,
            Column = targetToken.Column
        };
    }
}
