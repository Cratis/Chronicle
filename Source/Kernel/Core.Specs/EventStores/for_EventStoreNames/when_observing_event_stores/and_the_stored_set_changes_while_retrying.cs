// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;

namespace Cratis.Chronicle.EventStores.for_EventStoreNames.when_observing_event_stores;

public class and_the_stored_set_changes_while_retrying : given.a_live_event_store_list
{
    EventStoreName[] _newStores = ["replacement"];

    void Establish() => _initialization.EnsureAll(Arg.Is<IEnumerable<EventStoreName>>(stores => stores.SequenceEqual(_stores))).Returns([_stores[0]]);

    void Because()
    {
        Observe();
        _storedSets.OnNext(_stores);
        _storedSets.OnNext(_newStores);
        _scheduler.AdvanceBy(TimeSpan.FromMinutes(1).Ticks);
    }

    [Fact] void should_not_retry_the_superseded_set() => _initialization.Received(1).EnsureAll(Arg.Is<IEnumerable<EventStoreName>>(stores => stores.SequenceEqual(_stores)));
    [Fact] void should_initialize_the_new_set_once() => _initialization.Received(1).EnsureAll(Arg.Is<IEnumerable<EventStoreName>>(stores => stores.SequenceEqual(_newStores)));
    [Fact] void should_emit_the_new_set() => _emissions[^1].ShouldEqual([new EventStoreNames("replacement", "replacement")]);
    [Fact] void should_not_signal_an_error() => _errors.ShouldBeEmpty();
}
