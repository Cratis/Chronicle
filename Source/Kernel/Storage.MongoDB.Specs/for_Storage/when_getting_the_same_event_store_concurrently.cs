// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;

namespace Cratis.Chronicle.Storage.MongoDB.for_Storage;

public class when_getting_the_same_event_store_concurrently : given.a_storage
{
    static readonly EventStoreName _eventStore = "SomeEventStore";
    bool _firstCallerArrived;
    IEventStoreStorage _first;
    IEventStoreStorage _second;

    void Establish() => _database.RendezvousOnGetEventStoreDatabase = true;

    async Task Because()
    {
        var first = Task.Run(() => _storage.GetEventStore(_eventStore));
        _firstCallerArrived = _database.WaitForFirstCaller();
        var second = Task.Run(() => _storage.GetEventStore(_eventStore));
        _first = await first;
        _second = await second;
    }

    [Fact] void should_have_both_callers_inside_the_creation_path() => _firstCallerArrived.ShouldBeTrue();
    [Fact] void should_have_released_both_callers() => _database.CallersWereReleased.ShouldBeTrue();
    [Fact] void should_have_created_two_candidate_instances() => _database.EventStoreDatabaseCalls.ShouldEqual(2);
    [Fact] void should_return_the_same_instance_to_both_callers() => ReferenceEquals(_first, _second).ShouldBeTrue();
    [Fact] void should_return_the_cached_instance_on_a_later_call() => ReferenceEquals(_first, _storage.GetEventStore(_eventStore)).ShouldBeTrue();
}
