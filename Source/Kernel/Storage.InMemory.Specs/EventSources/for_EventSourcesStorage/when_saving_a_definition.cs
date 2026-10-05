// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.EventSources;

namespace Cratis.Chronicle.Storage.InMemory.EventSources.for_EventSourcesStorage;

public class when_saving_a_definition : given_an_event_sources_storage
{
    EventSourceDefinition? _found;
    EventSourceDefinition? _unknown;

    async Task Because()
    {
        await _storage.Save(Definition("ShoppingCart", "A cart", "Items", "Payment"));
        _found = await _storage.Find("ShoppingCart");
        _unknown = await _storage.Find("Nope");
    }

    [Fact] void should_find_it_by_name() => _found.ShouldNotBeNull();
    [Fact] void should_keep_the_streams() => _found!.Streams.Select(_ => _.Name.Value).ShouldContainOnly("Items", "Payment");
    [Fact] void should_keep_the_default_concurrency() => _found!.Concurrency.ShouldEqual(ConcurrencyDimensions.EventSourceId);
    [Fact] void should_not_find_an_unregistered_name() => _unknown.ShouldBeNull();
}
