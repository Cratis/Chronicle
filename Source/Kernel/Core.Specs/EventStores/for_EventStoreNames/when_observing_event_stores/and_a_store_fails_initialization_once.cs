// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;

namespace Cratis.Chronicle.EventStores.for_EventStoreNames.when_observing_event_stores;

public class and_a_store_fails_initialization_once : given.a_live_event_store_list
{
    void Establish() => _initialization.EnsureAll(Arg.Any<IEnumerable<EventStoreName>>()).Returns([_stores[0]], _stores);

    void Because()
    {
        Observe();
        _storedSets.OnNext(_stores);
        _scheduler.AdvanceBy(TimeSpan.FromSeconds(1).Ticks);
        _scheduler.AdvanceBy(TimeSpan.FromMinutes(1).Ticks);
    }

    [Fact] void should_emit_the_subset_then_the_full_list() => _emissions.Select(stores => string.Join(',', stores.Select(store => store.Name))).ShouldEqual(["ready", "ready,not-ready"]);
    [Fact] void should_stop_retrying_once_every_store_initializes() => _initialization.Received(2).EnsureAll(Arg.Any<IEnumerable<EventStoreName>>());
    [Fact] void should_not_signal_an_error() => _errors.ShouldBeEmpty();
}
