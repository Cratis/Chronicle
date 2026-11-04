// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Contracts.Observation;
using Cratis.Chronicle.Contracts.Observation.Reactors;
using Cratis.Chronicle.Observation;

namespace Cratis.Chronicle.Services.Observation.Reactors.for_Reactors;

public class when_receiving_a_result_for_a_page : given.a_page_observation_stream
{
    readonly TaskCompletionSource<ObserverSubscriberResult> _completion = new(TaskCreationOptions.RunContinuationsAsynchronously);
    ObserverSubscriberResult _result;

    void Establish() => _deliver("partition", [], _completion);

    async Task Because()
    {
        _messages.OnNext(new(new(new ReactorResult
        {
            DeliveryId = _deliveries[0].DeliveryId,
            Partitions =
            [
                new PartitionResult
                {
                    Partition = "partition",
                    State = ObservationState.Failed,
                    LastSuccessfulObservation = 5,
                    ExceptionMessages = ["Something went wrong"],
                    ExceptionStackTrace = string.Empty
                }
            ]
        })));
        _result = await _completion.Task.WaitAsync(TimeSpan.FromSeconds(5));
    }

    [Fact] void should_complete_the_partition_with_its_state() => _result.State.ShouldEqual(ObserverSubscriberState.Failed);
    [Fact] void should_carry_the_last_successful_observation() => _result.LastSuccessfulObservation.ShouldEqual((EventSequenceNumber)5UL);
    [Fact] void should_carry_the_exception_messages() => _result.ExceptionMessages.ShouldContainOnly("Something went wrong");
}
