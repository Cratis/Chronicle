// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

extern alias Client;

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.EventTypes;
using Cratis.Chronicle.Concepts.Projections;
using Cratis.Chronicle.Concepts.ReadModels;
using Cratis.Chronicle.Concepts.Sinks;
using Cratis.Chronicle.Projections.Engine.Expressions.EventValues;
using Cratis.Chronicle.Projections.Engine.Expressions.Keys;
using Cratis.Chronicle.Properties;
using Cratis.Chronicle.Schemas;
using Cratis.Serialization;
using Microsoft.Extensions.Logging.Abstractions;

using ClientComplianceMetadataResolver = Client::Cratis.Chronicle.Compliance.IComplianceMetadataResolver;
using ClientJsonSchemaGenerator = Client::Cratis.Chronicle.Schemas.JsonSchemaGenerator;

namespace Cratis.Chronicle.Projections.Engine.DeclarationLanguage.for_LanguageService.given;

public abstract class a_language_service_with_client_schemas<TReadModel> : Specification
    where TReadModel : class
{
    protected ILanguageService _languageService;
    protected ReadModelDefinition _readModelDefinition;
    protected List<EventTypeSchema> _eventTypeSchemas;

    void Establish()
    {
        var namingPolicy = new CamelCaseNamingPolicy();
        var generator = new ClientJsonSchemaGenerator(Substitute.For<ClientComplianceMetadataResolver>(), namingPolicy);
        var name = typeof(TReadModel).Name;
        _readModelDefinition = new ReadModelDefinition(
            new ReadModelIdentifier(name),
            new ReadModelContainerName(name),
            new ReadModelDisplayName(name),
            ReadModelOwner.Client,
            ReadModelSource.Code,
            ReadModelObserverType.Projection,
            ReadModelObserverIdentifier.Unspecified,
            new SinkDefinition(SinkConfigurationId.None, WellKnownSinkTypes.MongoDB),
            new Dictionary<ReadModelGeneration, JsonSchema>
            {
                [ReadModelGeneration.First] = generator.Generate(typeof(TReadModel))
            },
            []);
        _eventTypeSchemas =
        [
            new EventTypeSchema((EventType)nameof(UserAdded), EventTypeOwner.Client, EventTypeSource.Code, generator.Generate(typeof(UserAdded)))
        ];
        _languageService = new LanguageService(new Generator(), CodeGeneration.given.ProjectionCodeGenerators.All());
    }

    protected async Task<IDictionary<string, object>> ResolveKey(string expression, PropertyPath identifiedBy)
    {
        var projection = Substitute.For<IProjection>();
        projection.Identifier.Returns((ProjectionId)"FluentCompositeKey");
        projection.ReadModel.Returns(_readModelDefinition);
        var eventValueResolvers = Substitute.For<IEventValueProviderExpressionResolvers>();
        eventValueResolvers.Resolve(Arg.Any<JsonSchemaProperty>(), "userId").Returns(_ => "customer-42");
        eventValueResolvers.Resolve(Arg.Any<JsonSchemaProperty>(), "name").Returns(_ => "order-7");

        var resolver = new CompositeKeyExpressionResolver(eventValueResolvers, new KeyResolvers(NullLogger<KeyResolvers>.Instance));
        var result = await resolver.Resolve(projection, expression, identifiedBy)(null!, null!, null!);
        return (IDictionary<string, object>)((ResolvedKey)result).Key.Value;
    }
}
