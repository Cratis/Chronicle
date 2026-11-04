// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;

namespace Cratis.Chronicle.EventStores.for_EventStoreNames.when_observing_event_stores;

public class and_the_subscription_is_disposed_while_retrying : given.a_live_event_store_list
{
    void Establish() => _initialization.EnsureAll(Arg.Any<IEnumerable<EventStoreName>>()).Returns([_stores[0]]);

    void Because()
    {
        Observe();
        _storedSets.OnNext(_stores);
        _subscription.Dispose();
        _scheduler.AdvanceBy(TimeSpan.FromMinutes(1).Ticks);
    }

    [Fact] void should_not_retry_after_disposal() => _initialization.Received(1).EnsureAll(Arg.Any<IEnumerable<EventStoreName>>());
    [Fact] void should_not_emit_after_disposal() => _emissions.Count.ShouldEqual(1);
    [Fact] void should_not_signal_an_error() => _errors.ShouldBeEmpty();
}
