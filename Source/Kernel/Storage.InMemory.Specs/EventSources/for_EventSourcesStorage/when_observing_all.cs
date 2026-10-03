// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.InMemory.EventSources.for_EventSourcesStorage;

public class when_observing_all : given_an_event_sources_storage
{
    readonly List<int> _counts = [];

    async Task Because()
    {
        using var subscription = _storage.ObserveAll().Subscribe(all => _counts.Add(all.Count()));
        await _storage.Save(Definition("ShoppingCart"));
        await _storage.Save(Definition("Order"));
    }

    [Fact] void should_emit_the_current_state_first_and_then_every_change() => _counts.ShouldEqual([0, 1, 2]);
}
