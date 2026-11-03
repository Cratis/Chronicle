// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Contracts.Observation;
using Cratis.Chronicle.Contracts.Observation.Reducers;
using Cratis.Chronicle.Observation;
using Cratis.Chronicle.Observation.Reducers.Clients;

namespace Cratis.Chronicle.Services.Observation.Reducers.for_Reducers;

public class when_receiving_a_result_for_a_page : given.a_page_observation_stream
{
    readonly TaskCompletionSource<ReducerSubscriberResult> _completion = new(TaskCreationOptions.RunContinuationsAsynchronously);
    ReducerSubscriberResult _result;

    void Establish() => _deliver(new("partition", [], null), _completion);

    async Task Because()
    {
        _messages.OnNext(new(new(new ReducerResult
        {
            DeliveryId = _deliveries[0].DeliveryId,
            Partitions =
            [
                new PartitionResult
                {
                    Partition = "partition",
                    State = ObservationState.Success,
                    LastSuccessfulObservation = 9,
                    ExceptionStackTrace = string.Empty
                }
            ]
        })));
        _result = await _completion.Task.WaitAsync(TimeSpan.FromSeconds(5));
    }

    [Fact] void should_complete_the_partition_as_handled() => _result.ObserverResult.State.ShouldEqual(ObserverSubscriberState.Ok);
    [Fact] void should_carry_the_last_successful_observation() => _result.ObserverResult.LastSuccessfulObservation.ShouldEqual((EventSequenceNumber)9UL);
    [Fact] void should_carry_no_read_model_when_none_was_returned() => _result.ReadModelState.ShouldBeNull();
}
