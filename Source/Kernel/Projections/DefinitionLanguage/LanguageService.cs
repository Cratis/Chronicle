// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.EventSequences;
using Cratis.Chronicle.Concepts.Projections;
using Cratis.Chronicle.Concepts.Projections.Definitions;
using Cratis.Chronicle.Concepts.ReadModels;

namespace Cratis.Chronicle.Projections.DefinitionLanguage;

/// <summary>
/// Represents an implementation of the <see cref="ILanguageService"/>.
/// </summary>
/// <param name="generator">The generator used to generate projection language definition strings.</param>
public class LanguageService(IGenerator generator) : ILanguageService
{
    /// <inheritdoc/>
    public ProjectionDefinition Compile(
        string dsl,
        ProjectionId identifier,
        ProjectionOwner owner,
        EventSequenceId eventSequenceId)
    {
        var tokenizer = new Tokenizer(dsl);
        var tokens = tokenizer.Tokenize();
        var parser = new Parser(tokens);
        var document = parser.Parse();
        var compiler = new Compiler();
        return compiler.Compile(document, identifier, owner, eventSequenceId);
    }

    /// <inheritdoc/>
    public string Generate(ProjectionDefinition definition, ReadModelDefinition readModelDefinition) =>
        generator.Generate(definition, readModelDefinition)
}
