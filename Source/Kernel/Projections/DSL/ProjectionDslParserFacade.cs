// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.EventSequences;
using Cratis.Chronicle.Concepts.Projections;
using Cratis.Chronicle.Concepts.Projections.Definitions;

namespace Cratis.Chronicle.Projections.DSL;

/// <summary>
/// Simple façade that wires the <see cref="Tokenizer"/>, <see cref="RulesProjectionDslParser"/>, and <see cref="AstToProjectionDefinitionCompiler"/>
/// to provide a single entry point for parsing a DSL string into a <see cref="ProjectionDefinition"/>.
/// </summary>
public class ProjectionDslParserFacade : IProjectionDslParser
{
    /// <inheritdoc/>
    public ProjectionDefinition Parse(string dsl, ProjectionId identifier, ProjectionOwner owner, EventSequenceId eventSequenceId)
    {
        var tokenizer = new Tokenizer(dsl);
        var tokens = tokenizer.Tokenize().ToList();

        try
        {
            var parser = new RulesProjectionDslParser(tokens);
            var document = parser.Parse();
            var compiler = new AstToProjectionDefinitionCompiler();
            return compiler.Compile(document, identifier, owner, eventSequenceId);
        }
        catch (Exception ex)
        {
            var tokenList = string.Join(" | ", tokens.Select(t => $"{t.Line}:{t.Column}:{t.Type}('{t.Value}')"));
            throw new Exception($"Failed to parse DSL: {ex.Message}. Tokens: {tokenList}", ex);
        }
    }
}
