// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Projections.DefinitionLanguage.AST;

namespace Cratis.Chronicle.Projections.DefinitionLanguage.Visitors;

/// <summary>
/// Visitor for parsing sequence directives.
/// </summary>
internal sealed class SequenceDirectiveVisitor : IDirectiveVisitor
{
    /// <inheritdoc/>
    public ProjectionDirective? Visit(IParsingContext context)
    {
        if (!context.Check(TokenType.Sequence))
        {
            return null;
        }
        context.Advance();

        if (context.Expect(TokenType.Identifier) is not { } sequenceToken)
        {
            return null;
        }

        return new SequenceDirective(sequenceToken.Value);
    }
}
