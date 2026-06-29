// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;

namespace Cratis.Chronicle.Storage.InMemory.for_EventStoreStorages;

public class when_getting_storage_for_unknown_event_store : given.an_empty_registry
{
    static readonly EventStoreName _eventStore = "SomeEventStore";

    void Because() => _storages.GetOrCreate(_eventStore);

    [Fact] void should_report_having_the_event_store() => _storages.Has(_eventStore).ShouldBeTrue();
    [Fact] void should_include_the_event_store_in_names() => _storages.Names.ShouldContain(_eventStore);
}
