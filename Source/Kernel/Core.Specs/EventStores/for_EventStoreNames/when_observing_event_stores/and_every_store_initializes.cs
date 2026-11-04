// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;

namespace Cratis.Chronicle.EventStores.for_EventStoreNames.when_observing_event_stores;

public class and_every_store_initializes : given.a_live_event_store_list
{
    void Because()
    {
        Observe();
        _storedSets.OnNext(_stores);
        _scheduler.AdvanceBy(TimeSpan.FromMinutes(1).Ticks);
    }

    [Fact] void should_emit_once() => _emissions.Count.ShouldEqual(1);
    [Fact] void should_emit_every_store() => _emissions.Single().ShouldEqual([new EventStoreNames("ready", "ready"), new EventStoreNames("not-ready", "not-ready")]);
    [Fact] void should_initialize_once() => _initialization.Received(1).EnsureAll(Arg.Any<IEnumerable<EventStoreName>>());
}
