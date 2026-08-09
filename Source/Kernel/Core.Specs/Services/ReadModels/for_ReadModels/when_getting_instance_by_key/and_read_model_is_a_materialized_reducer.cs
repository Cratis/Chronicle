// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using System.Text.Json;
using System.Text.Json.Nodes;
using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Concepts.Sinks;
using Cratis.Chronicle.Contracts.ReadModels;
using Cratis.Chronicle.Storage;

namespace Cratis.Chronicle.Services.ReadModels.for_ReadModels.when_getting_instance_by_key;

public class and_read_model_is_a_materialized_reducer : given.all_dependencies
{
    GetInstanceByKeyResponse _result = null!;

    void Establish()
    {
        _readModelDefinition = _readModelDefinition with
        {
            ObserverType = Concepts.ReadModels.ReadModelObserverType.Reducer,
            ObserverIdentifier = "my-reducer",
            Sink = new SinkDefinition(SinkConfigurationId.None, WellKnownSinkTypes.InMemory)
        };
        _readModel.GetDefinition().Returns(_readModelDefinition);

        var storedState = new ExpandoObject();
        var storedValues = (IDictionary<string, object?>)storedState;
        storedValues["name"] = "FromMaterializedStore";
        storedValues[WellKnownProperties.LastHandledEventSequenceNumber] = 42UL;
        _sink.FindOrDefault(Arg.Any<Key>()).Returns(storedState);

        _expandoObjectConverter.ToJsonObject(Arg.Any<ExpandoObject>(), Arg.Any<Schemas.JsonSchema>())
            .Returns(call =>
            {
                var jsonObject = new JsonObject();
                foreach (var (key, value) in (IDictionary<string, object?>)call.Arg<ExpandoObject>())
                {
                    jsonObject[key] = value?.ToString();
                }

                return jsonObject;
            });
    }

    async Task Because() => _result = await _service.GetInstanceByKey(new()
    {
        EventStore = "test-store",
        Namespace = "test-namespace",
        ReadModelIdentifier = _readModelDefinition.Identifier,
        EventSequenceId = "event-log",
        ReadModelKey = "read-model-key"
    });

    [Fact] void should_return_the_materialized_read_model() => JsonSerializer.Deserialize<JsonElement>(_result.ReadModel).GetProperty("name").GetString().ShouldEqual("FromMaterializedStore");
    [Fact] void should_report_the_stored_last_handled_sequence_number() => _result.LastHandledEventSequenceNumber.ShouldEqual(42UL);
    [Fact] void should_not_project_any_events() => _result.ProjectedEventsCount.ShouldEqual(0UL);
    [Fact] void should_not_reduce_through_a_connected_client() => _reducerMediator.DidNotReceiveWithAnyArgs().OnNext(default!, default!, default!, default!, default!, default!);
}
