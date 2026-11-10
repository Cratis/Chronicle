// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Observation.Reducers.Clients;

namespace Cratis.Chronicle.Services.Observation.Reducers.for_Reducers;

/// <summary>
/// The state the client already holds is left out per partition of a page, so a page only carries the states that changed.
/// </summary>
public class when_a_page_holds_a_partition_whose_state_is_unchanged : given.a_paged_state_caching_observation_stream
{
    async Task Establish()
    {
        await _pagesSubscribed.Task.WaitAsync(TimeSpan.FromSeconds(5));
        _ = Deliver(null, "first");
        ReplyWithState(StateOf("first", 1), "first");
    }

    void Because() => _deliverPage(
        [
            new((Key)"first", [], StateOf("first", 1)),
            new((Key)"second", [], StateOf("second", 2))
        ],
        new Dictionary<Key, TaskCompletionSource<ReducerSubscriberResult>>
        {
            [(Key)"first"] = new(TaskCreationOptions.RunContinuationsAsynchronously),
            [(Key)"second"] = new(TaskCreationOptions.RunContinuationsAsynchronously)
        });

    [Fact] void should_mark_the_unchanged_partition_as_unchanged() => _deliveries[1].Partitions.Single(_ => _.Partition == "first").InitialStateUnchanged.ShouldBeTrue();
    [Fact] void should_not_send_the_unchanged_state() => _deliveries[1].Partitions.Single(_ => _.Partition == "first").InitialState.ShouldBeNull();
    [Fact] void should_send_the_state_the_client_does_not_hold() => _deliveries[1].Partitions.Single(_ => _.Partition == "second").InitialState.ShouldNotBeNull();
    [Fact] void should_not_mark_that_partition_as_unchanged() => _deliveries[1].Partitions.Single(_ => _.Partition == "second").InitialStateUnchanged.ShouldBeFalse();
}
