// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Projections;
using Cratis.Chronicle.Concepts.ReadModels;
using NJsonSchema;

namespace Cratis.Chronicle.Projections.DefinitionLanguage.for_LanguageService.given;

public class a_language_service : Specification
{
    protected ILanguageService _languageService;
    protected ProjectionId _projectionId;

    void Establish()
    {
        _languageService = new LanguageService(new Generator(), new DeclarativeCodeGenerator(), new ModelBoundCodeGenerator());
        _projectionId = new ProjectionId(Guid.NewGuid().ToString());
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
            [],
            ReadModelObserverType.Projection,
            ReadModelObserverIdentifier.Unspecified);
    }
}
