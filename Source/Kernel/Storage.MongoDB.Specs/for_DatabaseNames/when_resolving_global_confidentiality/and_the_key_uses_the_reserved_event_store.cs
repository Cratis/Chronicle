// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.ProtectedValues;

namespace Cratis.Chronicle.Storage.MongoDB.for_DatabaseNames.when_resolving_global_confidentiality;

public class and_the_key_uses_the_reserved_event_store : Specification
{
    Exception _exception;
    string _namespaceDatabase;
    Exception _collision;

    void Because()
    {
        _exception = Catch.Exception(() => DatabaseNames.ForEventStore(EncryptedValueKeyIdentifiers.GlobalEventStore));
        _namespaceDatabase = DatabaseNames.ForEventStoreNamespace(EncryptedValueKeyIdentifiers.GlobalEventStore, EncryptedValueKeyIdentifiers.GlobalNamespace, "test-");
        _collision = Catch.Exception(() => DatabaseNames.ForEventStore("!chronicle-encrypted!"));
    }

    [Fact] void should_accept_the_global_key_storage_coordinates() => _exception.ShouldBeNull();
    [Fact] void should_resolve_the_global_namespace_with_a_prefix() => _namespaceDatabase.ShouldEqual("test-!chronicle-encrypted!+es+!chronicle-encrypted!");
    [Fact] void should_reject_a_real_store_aliasing_the_reserved_database() => _collision.ShouldBeOfExactType<InvalidDatabaseName>();
}
