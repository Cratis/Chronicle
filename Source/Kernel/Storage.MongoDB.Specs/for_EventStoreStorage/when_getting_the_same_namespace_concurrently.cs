// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;

namespace Cratis.Chronicle.Storage.MongoDB.for_EventStoreStorage;

public class when_getting_the_same_namespace_concurrently : given.an_event_store_storage
{
    static readonly EventStoreNamespaceName _namespace = "SomeNamespace";
    bool _firstCallerArrived;
    IEventStoreNamespaceStorage _first;
    IEventStoreNamespaceStorage _second;

    void Establish() => _eventStoreDatabase.RendezvousOnGetNamespaceDatabase = true;

    async Task Because()
    {
        var first = Task.Run(() => _storage.GetNamespace(_namespace));
        _firstCallerArrived = _eventStoreDatabase.WaitForFirstCaller();
        var second = Task.Run(() => _storage.GetNamespace(_namespace));
        _first = await first;
        _second = await second;
    }

    [Fact] void should_have_both_callers_inside_the_creation_path() => _firstCallerArrived.ShouldBeTrue();
    [Fact] void should_have_released_both_callers() => _eventStoreDatabase.CallersWereReleased.ShouldBeTrue();
    [Fact] void should_have_created_two_candidate_instances() => _eventStoreDatabase.NamespaceDatabaseCalls.ShouldEqual(2);
    [Fact] void should_return_the_same_instance_to_both_callers() => ReferenceEquals(_first, _second).ShouldBeTrue();
    [Fact] void should_return_the_same_sinks_to_both_callers() => ReferenceEquals(_first.Sinks, _second.Sinks).ShouldBeTrue();
    [Fact] void should_return_the_cached_instance_on_a_later_call() => ReferenceEquals(_first, _storage.GetNamespace(_namespace)).ShouldBeTrue();
}
