// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;

namespace Cratis.Chronicle.EventStores.for_EventStoreNames.when_observing_event_stores;

public class and_the_stored_set_changes_while_a_retry_is_running : given.a_live_event_store_list_with_a_started_retry
{
    EventStoreName[] _newStores = ["replacement"];

    void Because()
    {
        Observe();
        _storedSets.OnNext(_stores);
        _storedSets.OnNext(_newStores);
        _startedRetries.FinishStartedCallbacks();
    }

    [Fact] void should_not_retry_the_superseded_set() => _initialization.Received(1).EnsureAll(Arg.Is<IEnumerable<EventStoreName>>(stores => stores.SequenceEqual(_stores)));
    [Fact] void should_end_with_the_new_set() => _emissions[^1].ShouldEqual([new EventStoreNames("replacement", "replacement")]);
}
