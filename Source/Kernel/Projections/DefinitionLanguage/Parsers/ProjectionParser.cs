// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Projections.DefinitionLanguage.AST;
using Cratis.Chronicle.Projections.DefinitionLanguage.Visitors;

namespace Cratis.Chronicle.Projections.DefinitionLanguage.Parsers;

/// <summary>
/// Parses projection nodes.
/// </summary>
internal class ProjectionParser
{
    readonly TypeRefParser _typeRefs = new();
    readonly ProjectionDirectiveParser _directives = new();

    public ProjectionNode? Parse(IParsingContext context)
    {
        if (!context.Check(TokenType.Projection))
        {
            context.ReportError("Expected 'projection'");
            return null;
        }
        context.Advance();

        var projectionName = _typeRefs.Parse(context);
        if (projectionName is null) return null;

        if (context.Expect(TokenType.Arrow) is null) return null;

        var readModelType = _typeRefs.Parse(context);
        if (readModelType is null) return null;

        if (context.Expect(TokenType.Indent) is null) return null;

        var directives = new List<ProjectionDirective>();
        while (!context.Check(TokenType.Dedent) && !context.IsAtEnd)
        {
            var directive = _directives.Parse(context);
            if (directive is not null)
            {
                directives.Add(directive);
            }
            else
            {
                // No directive matched, advance to avoid infinite loop
                context.Advance();
            }
        }

        if (context.Check(TokenType.Dedent))
        {
            context.Advance();
        }

        return new ProjectionNode(projectionName.Name, readModelType, directives);
    }
}
