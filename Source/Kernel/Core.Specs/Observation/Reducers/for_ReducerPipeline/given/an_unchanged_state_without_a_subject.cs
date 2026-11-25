// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using System.Text.Json.Nodes;
using Cratis.Chronicle.Changes;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Concepts.ReadModels;
using Cratis.Chronicle.Json;
using Cratis.Chronicle.ReadModels;
using Cratis.Chronicle.Schemas;

namespace Cratis.Chronicle.Observation.Reducers.for_ReducerPipeline.given;

public class an_unchanged_state_without_a_subject : all_dependencies
{
    protected ExpandoObject _initial;
    protected ReadModelChangeNotification _result;

    void Establish()
    {
        _schema.Properties["value"] = new JsonSchemaProperty { Type = JsonObjectType.Integer };
        _initial = State();
        _sink.FindOrDefault(Arg.Any<Key>()).Returns(_initial);
        _expandoObjectConverter = new ExpandoObjectConverter(new TypeFormats());
        _complianceManager.ApplyToReadModel(Arg.Any<EventStoreName>(), Arg.Any<EventStoreNamespaceName>(), Arg.Any<JsonSchema>(), Arg.Any<string>(), Arg.Any<JsonObject>())
            .Returns(call => Task.FromResult(call.ArgAt<JsonObject>(4)));
        _pipeline = new ReducerPipeline(_readModelDefinition, _sink, new ObjectComparer(), new ReadModelsCompliance(_complianceManager, _expandoObjectConverter), EventStore, EventStoreNamespace, () => Task.CompletedTask, expandoObjectConverter: _expandoObjectConverter);
    }

    protected async Task Reduce() => _result = await _pipeline.Reduce(CreateContext(EventSourceIdValue), CreateReducer(State()));

    static ExpandoObject State()
    {
        dynamic state = new ExpandoObject();
        state.value = 3L;
        return state;
    }
}
