// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Storage.Naming;

namespace Cratis.Chronicle.Storage.for_Storage;

public class when_getting_case_variants_of_an_event_store_concurrently : given.a_storage
{
    static readonly EventStoreName _eventStore = "SomeEventStore";
    static readonly EventStoreName _variant = "someeventstore";
    Task<IEventStoreStorage> _first;
    Task<IEventStoreStorage> _second;

    void Establish() => _clusterStorage.RendezvousOnGetEventStores = true;

    async Task Because()
    {
        _first = Task.Run(() => _storage.GetEventStore(_eventStore));
        _clusterStorage.WaitForFirstCaller();
        _second = Task.Run(() => _storage.GetEventStore(_variant));
        await Task.WhenAll(_first, _second).ContinueWith(_ => { }, TaskScheduler.Default);
    }

    [Fact] void should_serve_exactly_one_of_them() => new[] { _first, _second }.Count(_ => _.IsCompletedSuccessfully).ShouldEqual(1);
    [Fact] void should_refuse_the_other_as_a_case_variant() => ((ResourceNameConflict)new[] { _first, _second }.Single(_ => _.IsFaulted).Exception!.InnerException!).Kind.ShouldEqual(ResourceNameConflictKind.EventStoreCaseVariant);
}
