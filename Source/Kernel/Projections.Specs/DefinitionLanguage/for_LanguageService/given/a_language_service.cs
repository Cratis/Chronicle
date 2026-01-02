// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.EventSequences;
using Cratis.Chronicle.Concepts.Projections;
using Cratis.Chronicle.Concepts.Projections.Definitions;
using Cratis.Chronicle.Concepts.ReadModels;
using NJsonSchema;

namespace Cratis.Chronicle.Projections.DefinitionLanguage.for_LanguageService.given;

public class a_language_service : Specification
{
    protected ILanguageService _languageService;
    protected ProjectionId _projectionId;

    void Establish()
    {
        _languageService = new LanguageService(new Generator());
        _projectionId = new ProjectionId(Guid.NewGuid().ToString());
    }

    protected ProjectionDefinition CompileGenerateAndRecompile(string definition, string readModelName)
    {
        var result = _languageService.Compile(
            definition,
            _projectionId,
            ProjectionOwner.Client,
            EventSequenceId.Log);
        var compiled = result.Match(
            projectionDef => projectionDef,
            errors => throw new InvalidOperationException($"Compilation failed: {string.Join(", ", errors.Errors)}"));

        var readModelDefinition = CreateReadModelDefinition(readModelName);
        var generated = _languageService.Generate(compiled, readModelDefinition);

        var recompileResult = _languageService.Compile(
            generated,
            _projectionId,
            ProjectionOwner.Client,
            EventSequenceId.Log);
        return recompileResult.Match(
            projectionDef => projectionDef,
            errors => throw new InvalidOperationException($"Re-compilation of generated DSL failed: {string.Join(", ", errors.Errors)}"));
    }

    ReadModelDefinition CreateReadModelDefinition(string name)
    {
        var schema = new JsonSchema
        {
            Title = name
        };

        return new ReadModelDefinition(
            new ReadModelIdentifier(Guid.NewGuid().ToString()),
            new ReadModelName(name),
            ReadModelOwner.Client,
            new Concepts.Sinks.SinkDefinition(
                new Concepts.Sinks.SinkConfigurationId(Guid.NewGuid()),
                Concepts.Sinks.WellKnownSinkTypes.MongoDB),
            new Dictionary<ReadModelGeneration, JsonSchema>
            {
                [ReadModelGeneration.First] = schema
            },
            []);
    }
}
