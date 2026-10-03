// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.EventSources;

namespace Cratis.Chronicle.Storage.InMemory.EventSources.for_EventSourcesStorage;

public class when_saving_a_definition_again : given_an_event_sources_storage
{
    IEnumerable<EventSourceDefinition> _all;

    async Task Because()
    {
        await _storage.Save(Definition("ShoppingCart", "Old", "Items"));
        await _storage.Save(Definition("Order"));
        await _storage.Save(Definition("ShoppingCart", "New", "Items", "Payment"));
        _all = await _storage.GetAll();
    }

    [Fact] void should_replace_the_definition_rather_than_duplicate_it() => _all.Count(_ => _.Name.Value == "ShoppingCart").ShouldEqual(1);
    [Fact] void should_use_the_latest_description() => _all.Single(_ => _.Name.Value == "ShoppingCart").Description.Value.ShouldEqual("New");
    [Fact] void should_keep_definitions_that_are_not_registered_again() => _all.Any(_ => _.Name.Value == "Order").ShouldBeTrue();
}
