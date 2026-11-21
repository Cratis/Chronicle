// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Storage.Observation;

namespace Cratis.Chronicle.Observation.for_UnseenObserverRemoval;

public class when_an_observer_is_dormant : given.an_unseen_observer_removal
{
    int _removedCount;

    protected override IDurableSubscriptionRecords? Records
    {
        get
        {
            var records = Substitute.For<IDurableSubscriptionRecords>();
            records.CanBeRelied.Returns(true);
            records.Get(Arg.Any<ObserverKey>()).Returns(call =>
                Task.FromResult<ObserverDurableSubscription?>(call.Arg<ObserverKey>().ObserverId == "dormant"
                    ? new ObserverDurableSubscription(ObserverSubscriptionKind.ClientReactor, false, null, [new("a-connection", "silo")])
                    : null));
            return records;
        }
    }

    async Task Because() => _removedCount = await _removal.Sweep();

    [Fact] void should_remove_only_the_observer_nobody_holds() => _removedCount.ShouldEqual(1);
    [Fact] void should_not_remove_the_dormant_observer() => _removed.ShouldContainOnly("abandoned");
    [Fact] void should_not_wake_the_dormant_observer_to_ask_whether_it_is_subscribed() => _grains[new ObserverKey("dormant", EventStore, First, Concepts.EventSequences.EventSequenceId.Log)].DidNotReceive().IsSubscribed();
    [Fact] async Task should_count_the_dormant_observer_as_seen_now() => (await _states[First].GetLastSeenAt(["dormant"], _time.Now))["dormant"].ShouldEqual(_time.Now);

    protected override async Task Seed()
    {
        await Observer("dormant", Old, Old);
        await Observer("abandoned", Old, Old);
    }
}
