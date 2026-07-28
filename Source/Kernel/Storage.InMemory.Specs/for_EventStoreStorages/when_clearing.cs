// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;

namespace Cratis.Chronicle.Storage.InMemory.for_EventStoreStorages;

public class when_clearing : given.an_empty_registry
{
    static readonly EventStoreName _eventStore = "SomeEventStore";

    void Establish() => _storages.GetOrCreate(_eventStore);

    void Because() => _storages.Clear();

    [Fact] void should_no_longer_have_the_event_store() => _storages.Has(_eventStore).ShouldBeFalse();
    [Fact] void should_have_no_names() => _storages.Names.ShouldBeEmpty();
}
