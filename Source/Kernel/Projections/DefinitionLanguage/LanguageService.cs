// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.EventTypes;
using Cratis.Chronicle.Concepts.Projections;
using Cratis.Chronicle.Concepts.Projections.Definitions;
using Cratis.Chronicle.Concepts.ReadModels;
using Cratis.Monads;

namespace Cratis.Chronicle.Projections.DefinitionLanguage;

/// <summary>
/// Represents an implementation of the <see cref="ILanguageService"/>.
/// </summary>
/// <param name="generator">The generator used to generate projection language definition strings.</param>
/// <param name="declarativeCodeGenerator">The generator for declarative C# projection code.</param>
/// <param name="modelBoundCodeGenerator">The generator for model-bound C# read model code.</param>
public class LanguageService(
    IGenerator generator,
    DeclarativeCodeGenerator declarativeCodeGenerator,
    ModelBoundCodeGenerator modelBoundCodeGenerator) : ILanguageService
{
    /// <inheritdoc/>
    public Result<ProjectionDefinition, CompilerErrors> Compile(
        string definition,
        ProjectionOwner owner,
        IEnumerable<ReadModelDefinition> readModelDefinitions,
        IEnumerable<EventTypeSchema> eventTypeSchemas)
    {
        var tokenizer = new Tokenizer(definition);
        var tokenizeResult = tokenizer.Tokenize();

        return tokenizeResult.Match(
            tokens =>
            {
                var parser = new Parser(tokens);
                var parseResult = parser.Parse();

                return parseResult.Match(
                    document =>
                    {
                        var compiler = new Compiler();
                        return compiler.Compile(document, owner, readModelDefinitions, eventTypeSchemas);
                    },
                    parsingErrors => CompilerErrors.FromParsingErrors(parsingErrors));
            },
            parsingErrors => CompilerErrors.FromParsingErrors(parsingErrors));
    }

    /// <inheritdoc/>
    public string Generate(ProjectionDefinition definition, ReadModelDefinition readModelDefinition) =>
        generator.Generate(definition, readModelDefinition);

    /// <inheritdoc/>
    public Result<ReadModelIdentifier, CompilerErrors> GetReadModelIdentifier(string definition)
    {
        var tokenizer = new Tokenizer(definition);
        var tokenizeResult = tokenizer.Tokenize();

        return tokenizeResult.Match(
            tokens =>
            {
                var parser = new Parser(tokens);
                var parseResult = parser.Parse();

                return parseResult.Match(
                    document =>
                    {
                        var compiler = new Compiler();
                        return compiler.GetReadModelIdentifier(document);
                    },
                    parsingErrors => CompilerErrors.FromParsingErrors(parsingErrors));
            },
            parsingErrors => CompilerErrors.FromParsingErrors(parsingErrors));
    }

    /// <inheritdoc/>
    public string GenerateDeclarativeCode(ProjectionDefinition definition, ReadModelDefinition readModelDefinition) =>
        declarativeCodeGenerator.Generate(definition, readModelDefinition);

    /// <inheritdoc/>
    public string GenerateModelBoundCode(ProjectionDefinition definition, ReadModelDefinition readModelDefinition) =>
        modelBoundCodeGenerator.Generate(definition, readModelDefinition).ToFullString();
}
