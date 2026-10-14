// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using System.Text.Json;
using System.Text.Json.Nodes;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.ReadModels;
using Cratis.Chronicle.Contracts.ReadModels;
using Cratis.Chronicle.Json;
using Cratis.Chronicle.Observation.Reducers.Clients;
using Cratis.Chronicle.ReadModels;
using Cratis.Chronicle.Schemas;

namespace Cratis.Chronicle.Projections.for_ImmediateProjection.given;

public class a_never_created_projection : an_immediate_projection
{
    protected ProjectionResult _result;
    protected GetInstanceByKeyResponse _clientResult;
    protected Contracts.ReadModels.IReadModels _service;
    protected virtual JsonObject InitialModelState => new();

    async Task Establish()
    {
        var schema = await JsonSchema.FromJsonAsync("""
            { "type": "object", "properties": {
              "id": { "type": "string" }, "name": { "type": "string" },
              "value": { "type": "integer" }, "enabled": { "type": "boolean" },
              "children": { "type": "array", "items": { "type": "string" } }
            } }
            """);
        var definition = await _readModel.GetDefinition();
        definition = definition with { Schemas = new Dictionary<ReadModelGeneration, JsonSchema> { [ReadModelGeneration.First] = schema } };
        _readModel.GetDefinition().Returns(definition);
        var converter = new ExpandoObjectConverter(new TypeFormats());
        _expandoObjectConverter.ToExpandoObject(Arg.Any<JsonObject>(), Arg.Any<JsonSchema>())
            .Returns(call => converter.ToExpandoObject(call.Arg<JsonObject>(), call.Arg<JsonSchema>()));
        _expandoObjectConverter.ToJsonObject(Arg.Any<ExpandoObject>(), Arg.Any<JsonSchema>())
            .Returns(call => converter.ToJsonObject(call.Arg<ExpandoObject>(), call.Arg<JsonSchema>()));
        await _grain.OnProjectionDefinitionsChanged(CreateProjectionDefinition() with { IsActive = false, InitialModelState = InitialModelState });
        _projection.GetEventTypes().Returns([new EventType("Created", EventTypeGeneration.First)]);
        _eventSequence.GetTailSequenceNumberForEventTypes(Arg.Any<IEnumerable<EventType>>()).Returns(EventSequenceNumber.Unavailable);
        _eventSequenceStorage.GetFromSequenceNumber(EventSequenceNumber.First, eventSourceId: ReadModelKey, eventTypes: Arg.Any<IEnumerable<EventType>>())
            .Returns(_ => CreateCursor());

        var grainFactory = Substitute.For<IGrainFactory>();
        grainFactory.GetGrain<IReadModel>(Arg.Any<string>()).Returns(_readModel);
        grainFactory.GetGrain<IImmediateProjection>(Arg.Any<string>()).Returns(_grain);
        var compliance = Substitute.For<IReadModelsCompliance>();
        compliance.ReleaseJson(Arg.Any<EventStoreName>(), Arg.Any<EventStoreNamespaceName>(), Arg.Any<JsonSchema>(), Arg.Any<JsonObject>())
            .Returns(call => call.Arg<JsonObject>());
        _service = new Services.ReadModels.ReadModels(
            grainFactory,
            Substitute.For<Storage.IStorage>(),
            converter,
            Substitute.For<IReducerMediator>(),
            Substitute.For<IProjectionChangesetMediator>(),
            Substitute.For<ILocalSiloDetails>(),
            compliance,
            Substitute.For<IMaterializedReadModelStore>(),
            new JsonSerializerOptions());
    }

    protected async Task Read()
    {
        _result = await _grain.GetModelInstance();
        _clientResult = await _service.GetInstanceByKey(new()
        {
            EventStore = EventStore,
            Namespace = EventStoreNamespaceName.Default,
            ReadModelIdentifier = ReadModel,
            EventSequenceId = "event-log",
            ReadModelKey = ReadModelKey
        });
    }
}
