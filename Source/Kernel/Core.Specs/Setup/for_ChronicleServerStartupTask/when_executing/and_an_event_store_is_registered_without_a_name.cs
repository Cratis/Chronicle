// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;

namespace Orleans.Hosting.for_ChronicleServerStartupTask.when_executing;

/// <summary>
/// The cluster's event store registry holds an entry whose name is empty (Cratis/Chronicle#5422). It is skipped, and the
/// event stores that have a name still start.
/// </summary>
public class and_an_event_store_is_registered_without_a_name : given.a_startup_task
{
    static readonly EventStoreName _unnamed = new(string.Empty);
    Exception _exception;

    void Establish() => _storage.GetEventStores().Returns(Task.FromResult<IEnumerable<EventStoreName>>([_unnamed, _eventStore]));

    async Task Because() => _exception = await Catch.Exception(Execute);

    [Fact] void should_start() => _exception.ShouldBeNull();
    [Fact] void should_not_register_event_types_for_it() => _eventTypes.DidNotReceive().DiscoverAndRegister(_unnamed);
    [Fact] void should_still_register_event_types_for_the_named_event_store() => _eventTypes.Received(1).DiscoverAndRegister(_eventStore);
}
