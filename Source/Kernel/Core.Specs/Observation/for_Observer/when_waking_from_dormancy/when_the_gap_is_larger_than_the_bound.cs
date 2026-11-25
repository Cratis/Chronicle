// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Observation.Jobs;

namespace Cratis.Chronicle.Observation.for_Observer.when_waking_from_dormancy;

public class when_the_gap_is_larger_than_the_bound : given.a_dormant_client_observer
{
    protected override int ResumeMaxEvents => 3;

    void Establish() => Append(Enumerable.Range(0, 5).Select(index => EventAt((ulong)index)).ToArray());

    async Task Because() => await Wake();

    [Fact] void should_not_deliver_inside_the_activation() => _delivered.Count.ShouldEqual(0);
    [Fact] void should_fall_back_to_the_catch_up_path() => _jobsManager.Received().Start<ICatchUpObserver, CatchUpObserverRequest>(Arg.Any<CatchUpObserverRequest>());
    [Fact] void should_keep_the_cursor_where_it_was() => _stateStorage.State.NextEventSequenceNumber.Value.ShouldEqual(0UL);
}
