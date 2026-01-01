// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.EventSequences;
using Cratis.Chronicle.Concepts.Projections;
using Cratis.Chronicle.Concepts.Projections.Definitions;

namespace Cratis.Chronicle.Projections.DSL;

/// <summary>
/// Provides functionality to parse projection DSL into ProjectionDefinition objects.
/// </summary>
public static class ProjectionDsl
{
    /// <summary>
    /// Parses a projection DSL string into a ProjectionDefinition.
    /// </summary>
    /// <param name="dsl">The DSL string to parse.</param>
    /// <param name="identifier">The projection identifier.</param>
    /// <param name="owner">The projection owner.</param>
    /// <param name="eventSequenceId">The event sequence identifier.</param>
    /// <returns>A ProjectionDefinition.</returns>
    public static ProjectionDefinition Parse(
        string dsl,
        ProjectionId identifier,
        ProjectionOwner owner,
        EventSequenceId eventSequenceId)
    {
        var tokenizer = new Tokenizer(dsl);
        var tokens = tokenizer.Tokenize();
        var parser = new RulesProjectionDslParser(tokens);
        var document = parser.Parse();
        var compiler = new AstToProjectionDefinitionCompiler();
        return compiler.Compile(document, identifier, owner, eventSequenceId);
    }
}
