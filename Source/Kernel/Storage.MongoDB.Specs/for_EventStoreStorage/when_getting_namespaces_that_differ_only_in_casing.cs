// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;

namespace Cratis.Chronicle.Storage.MongoDB.for_EventStoreStorage;

public class when_getting_namespaces_that_differ_only_in_casing : given.an_event_store_storage
{
    static readonly EventStoreNamespaceName _namespace = "MyNamespace";
    static readonly EventStoreNamespaceName _differentlyCasedNamespace = "mynamespace";
    IEventStoreNamespaceStorage _first;
    IEventStoreNamespaceStorage _second;

    void Establish() => _first = _storage.GetNamespace(_namespace);

    void Because() => _second = _storage.GetNamespace(_differentlyCasedNamespace);

    [Fact] void should_treat_them_as_separate_namespaces() => ReferenceEquals(_first, _second).ShouldBeFalse();
    [Fact] void should_create_storage_for_both() => _eventStoreDatabase.NamespaceDatabaseCalls.ShouldEqual(2);
}
