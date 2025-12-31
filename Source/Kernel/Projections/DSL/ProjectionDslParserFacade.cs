// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.EventSequences;
using Cratis.Chronicle.Concepts.Projections;
using Cratis.Chronicle.Concepts.Projections.Definitions;

namespace Cratis.Chronicle.Projections.DSL;

/// <summary>
/// Simple façade that wires the existing <see cref="Tokenizer"/> and <see cref="ProjectionDslParser"/>
/// to provide a single entry point for parsing a DSL string into a <see cref="ProjectionDefinition"/>.
/// </summary>
public class ProjectionDslParserFacade : IProjectionDslParser
{
    /// <inheritdoc/>
    public ProjectionDefinition Parse(string dsl, ProjectionId identifier, ProjectionOwner owner, EventSequenceId eventSequenceId)
    {
        var tokenizer = new Tokenizer(dsl);
        var tokens = tokenizer.Tokenize();
        var parser = new ProjectionDslParser(tokens);
        return parser.Parse(identifier, owner, eventSequenceId);
    }
}
