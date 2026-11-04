// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Storage.Naming;

namespace Cratis.Chronicle.Storage.for_Storage;

public class when_getting_a_case_variant_of_a_cached_event_store : given.a_storage
{
    static readonly EventStoreName _eventStore = "MyStore";
    static readonly EventStoreName _differentlyCasedEventStore = "mystore";
    IEventStoreStorage _first;
    IEventStoreStorage _again;
    Exception _error;

    void Establish() => _first = _storage.GetEventStore(_eventStore);

    void Because()
    {
        _error = Catch.Exception(() => _storage.GetEventStore(_differentlyCasedEventStore));
        _again = _storage.GetEventStore(_eventStore);
    }

    [Fact] void should_refuse_the_variant_as_a_case_variant() => ((ResourceNameConflict)_error).Kind.ShouldEqual(ResourceNameConflictKind.EventStoreCaseVariant);
    [Fact] void should_keep_serving_the_owner_from_the_cache() => ReferenceEquals(_first, _again).ShouldBeTrue();
    [Fact] void should_only_create_storage_once() => _clusterStorage.CreatedStorages.Count().ShouldEqual(1);
}
