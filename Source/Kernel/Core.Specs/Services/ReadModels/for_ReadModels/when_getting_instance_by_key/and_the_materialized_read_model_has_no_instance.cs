// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Concepts.Sinks;
using Cratis.Chronicle.Contracts.ReadModels;

namespace Cratis.Chronicle.Services.ReadModels.for_ReadModels.when_getting_instance_by_key;

public class and_the_materialized_read_model_has_no_instance : given.all_dependencies
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

        _sink.FindOrDefault(Arg.Any<Key>()).Returns((ExpandoObject?)null);
    }

    async Task Because() => _result = await _service.GetInstanceByKey(new()
    {
        EventStore = "test-store",
        Namespace = "test-namespace",
        ReadModelIdentifier = _readModelDefinition.Identifier,
        EventSequenceId = "event-log",
        ReadModelKey = "read-model-key"
    });

    [Fact] void should_answer_with_a_null_read_model() => _result.ReadModel.ShouldEqual("null");
    [Fact] void should_report_the_sequence_number_as_unavailable() => _result.LastHandledEventSequenceNumber.ShouldEqual(EventSequenceNumber.Unavailable.Value);
    [Fact] void should_not_fall_back_to_reducing_it() => _reducerMediator.DidNotReceiveWithAnyArgs().OnNext(default!, default!, default!, default!, default!, default!);
}
