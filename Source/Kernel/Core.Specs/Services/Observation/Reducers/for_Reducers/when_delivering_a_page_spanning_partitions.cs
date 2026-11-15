// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Contracts.Observation;
using Cratis.Chronicle.Contracts.Observation.Reducers;
using Cratis.Chronicle.Observation;
using Cratis.Chronicle.Observation.Reducers.Clients;

namespace Cratis.Chronicle.Services.Observation.Reducers.for_Reducers;

public class when_delivering_a_page_spanning_partitions : given.a_page_observation_stream
{
    Dictionary<Key, TaskCompletionSource<ReducerSubscriberResult>> _completions;
    ReducerSubscriberResult _first;
    ReducerSubscriberResult _second;

    async Task Establish()
    {
        await _pagesSubscribed.Task.WaitAsync(TimeSpan.FromSeconds(5));
        _completions = new()
        {
            [(Key)"first"] = new(TaskCreationOptions.RunContinuationsAsynchronously),
            [(Key)"second"] = new(TaskCreationOptions.RunContinuationsAsynchronously)
        };
    }

    async Task Because()
    {
        _deliverPage(
            [
                new((Key)"first", [], null) { Origin = Concepts.Observation.ObservationOrigin.Replay },
                new((Key)"second", [], new ExpandoObject()) { Origin = Concepts.Observation.ObservationOrigin.Replay }
            ],
            _completions);

        _messages.OnNext(new(new(new ReducerResult
        {
            DeliveryId = _deliveries[0].DeliveryId,
            Partitions =
            [
                new PartitionResult { Partition = "second", State = ObservationState.Failed, LastSuccessfulObservation = 3, ExceptionMessages = ["boom"], ExceptionStackTrace = string.Empty },
                new PartitionResult { Partition = "first", State = ObservationState.Success, LastSuccessfulObservation = 7, ExceptionStackTrace = string.Empty }
            ]
        })));
        _first = await _completions[(Key)"first"].Task.WaitAsync(TimeSpan.FromSeconds(5));
        _second = await _completions[(Key)"second"].Task.WaitAsync(TimeSpan.FromSeconds(5));
    }

    [Fact] void should_send_one_message() => _deliveries.Count.ShouldEqual(1);
    [Fact] void should_send_every_partition_in_it() => _deliveries[0].Partitions.Select(_ => _.Partition).ShouldContainOnly("first", "second");
    [Fact] void should_send_the_initial_state_of_a_partition_that_has_one() => _deliveries[0].Partitions.Single(_ => _.Partition == "second").InitialState.ShouldNotBeNull();
    [Fact] void should_send_no_initial_state_for_a_partition_without_one() => _deliveries[0].Partitions.Single(_ => _.Partition == "first").InitialState.ShouldBeNull();
    [Fact] void should_not_send_the_single_partition_fields() => _deliveries[0].Partition.ShouldBeNull();
    [Fact] void should_tell_the_client_the_origin() => _deliveries[0].Origin.ShouldEqual(ObservationOrigin.Replaying);
    [Fact] void should_settle_the_first_partition_on_its_own() => _first.ObserverResult.State.ShouldEqual(ObserverSubscriberState.Ok);
    [Fact] void should_settle_the_second_partition_on_its_own() => _second.ObserverResult.State.ShouldEqual(ObserverSubscriberState.Failed);
}
