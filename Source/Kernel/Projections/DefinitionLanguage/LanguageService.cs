// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.EventSequences;
using Cratis.Chronicle.Concepts.Projections;
using Cratis.Chronicle.Concepts.Projections.Definitions;
using Cratis.Chronicle.Concepts.ReadModels;
using Cratis.Monads;

namespace Cratis.Chronicle.Projections.DefinitionLanguage;

/// <summary>
/// Represents an implementation of the <see cref="ILanguageService"/>.
/// </summary>
/// <param name="generator">The generator used to generate projection language definition strings.</param>
public class LanguageService(IGenerator generator) : ILanguageService
{
    /// <inheritdoc/>
    public Result<ProjectionDefinition, ParsingErrors> Compile(
        string definition,
        ProjectionId identifier,
        ProjectionOwner owner,
        EventSequenceId eventSequenceId)
    {
        try
        {
            var tokenizer = new Tokenizer(definition);
            var tokens = tokenizer.Tokenize();
            var parser = new Parser(tokens);
            var parseResult = parser.Parse();

            return parseResult.Match<Result<ProjectionDefinition, ParsingErrors>>(
                document =>
                {
                    var compiler = new Compiler();
                    return compiler.Compile(document, identifier, owner, eventSequenceId);
                },
                errors => errors);
        }
        catch (InvalidOperationException ex)
        {
            // Handle tokenizer errors
            var errors = new ParsingErrors([]);
            errors.Add(ex.Message, 0, 0);
            return errors;
        }
    }

    /// <inheritdoc/>
    public string Generate(ProjectionDefinition definition, ReadModelDefinition readModelDefinition) =>
        generator.Generate(definition, readModelDefinition);
}
