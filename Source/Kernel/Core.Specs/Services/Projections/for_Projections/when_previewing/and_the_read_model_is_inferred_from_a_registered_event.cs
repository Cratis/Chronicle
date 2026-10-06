// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using System.Text.Json.Nodes;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.EventTypes;
using Cratis.Chronicle.Concepts.ReadModels;
using Cratis.Chronicle.Contracts.Primitives;
using Cratis.Chronicle.Contracts.Projections;
using Cratis.Chronicle.Json;
using Cratis.Chronicle.Projections.Engine.DeclarationLanguage;
using Cratis.Chronicle.Schemas;
using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Chronicle.Services.Projections.for_Projections.when_previewing;

public class and_the_read_model_is_inferred_from_a_registered_event : given.all_dependencies
{
    OneOf<ProjectionPreview, ProjectionDeclarationParsingErrors> _result;

    void Establish()
    {
        SetReadModels();
        var eventType = new EventType("VariantIssueOpened", EventTypeGeneration.First);
        var eventSchema = JsonSchema.FromJson("""
            { "$schema": "http://json-schema.org/draft-07/schema#", "title": "VariantIssueOpened",
              "type": "object", "properties": { "title": { "type": "string" } }, "required": ["title"] }
            """);
        _eventTypesStorage.GetLatestForAllEventTypes().Returns([new EventTypeSchema(eventType, EventTypeOwner.Client, EventTypeSource.Code, eventSchema)]);
        var projected = new ExpandoObject();
        var properties = (IDictionary<string, object?>)projected;
        properties["title"] = "preview";
        properties["_id"] = "the-source";
        _projectionGrain.ProcessForPreview(Arg.Any<EventStoreNamespaceName>(), Arg.Any<IEnumerable<AppendedEvent>>(), Arg.Any<ReadModelDefinition>())
            .Returns([projected]);
        _readModelsCompliance.Release(Arg.Any<EventStoreName>(), Arg.Any<EventStoreNamespaceName>(), Arg.Any<JsonSchema>(), Arg.Any<ExpandoObject>())
            .Returns(call => call.Arg<ExpandoObject>());
        var typeFormats = new TypeFormats();
        var language = new LanguageService(new Generator(), Cratis.Chronicle.Projections.Engine.DeclarationLanguage.CodeGeneration.given.ProjectionCodeGenerators.All());
        var services = new ServiceCollection();
        services.AddSingleton(_storage);
        _service = new Projections(_grainFactory, new ExpandoObjectConverter(typeFormats), language, services.BuildServiceProvider(), _readModelsCompliance);
    }

    async Task Because() => _result = await _service.Preview(new PreviewProjectionRequest
    {
        EventStore = EventStore,
        Namespace = EventStoreNamespace,
        EventSequenceId = "event-log",
        Declaration = """
            projection PreviewIssues
              from VariantIssueOpened
                title = title
            """
    });

    [Fact] void should_infer_the_projected_property_schema() => JsonSchema.FromJson(_result.Value0.ReadModel.Schema).Properties.ContainsKey("title").ShouldBeTrue();
    [Fact] void should_preserve_the_projected_property_in_the_response() => (JsonNode.Parse(_result.Value0.ReadModelEntries.Single())!["title"]?.GetValue<string>()).ShouldEqual("preview");
}
