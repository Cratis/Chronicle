// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Storage;

namespace Cratis.Chronicle.EventStores.for_EventStoreInitialization.when_ensuring_all;

public class and_one_store_fails : given.an_event_store_initialization
{
    IEnumerable<EventStoreName> _result;

    void Establish()
    {
        _storage.GetEventStore("a").Returns(Substitute.For<IEventStoreStorage>());
        _storage.GetEventStore("b").Returns(Substitute.For<IEventStoreStorage>());
        var failingInitializer = Substitute.For<IEventStoreInitializer>();
        failingInitializer.Ensure().Returns(Task.FromException(new EventStoreInitializationFailed("b")));
        _grains.GetGrain<IEventStoreInitializer>("b").Returns(failingInitializer);
    }

    async Task Because() => _result = await _initialization.EnsureAll(["a", "b"]);

    [Fact] void should_return_only_the_successfully_initialized_stores() => _result.ShouldEqual([(EventStoreName)"a"]);
}
