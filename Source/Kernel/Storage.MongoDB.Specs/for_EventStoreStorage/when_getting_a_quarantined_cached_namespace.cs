// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Storage.Naming;

namespace Cratis.Chronicle.Storage.MongoDB.for_EventStoreStorage;

public class when_getting_a_quarantined_cached_namespace : given.an_event_store_storage_with_name_checks
{
    Exception _error;

    void Establish()
    {
        _storage.GetNamespace("tenant");
        _snapshot.Publish([], [(_eventStore, new EventStoreNamespaceName("tenant"))]);
    }

    void Because() => _error = Catch.Exception(() => _storage.GetNamespace("tenant"));

    [Fact] void should_refuse_it_although_it_is_cached() => _error.ShouldBeOfExactType<ResourceQuarantined>();
}
