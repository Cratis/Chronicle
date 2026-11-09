// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;

namespace Cratis.Chronicle.Storage.MongoDB.for_DatabaseNames;

/// <summary>
/// A namespace cannot place read models in the event store database, and a prefix cannot complete an internal name for a registered store.
/// </summary>
public class when_a_namespace_reaches_the_event_store_database : Specification
{
    Exception _namespace;
    Exception _namespaceUpper;
    Exception _prefixedNamespace;
    Exception _registration;
    Exception _namespaceRegistration;
    string _other;

    void Because()
    {
        _namespace = Catch.Exception(() => DatabaseNames.ForReadModels(new EventStoreName("a"), new EventStoreNamespaceName("es")));
        _namespaceUpper = Catch.Exception(() => DatabaseNames.ForReadModels(new EventStoreName("a"), new EventStoreNamespaceName("ES")));
        _prefixedNamespace = Catch.Exception(() => DatabaseNames.ForReadModels(new EventStoreName("a"), new EventStoreNamespaceName("es"), "run_"));
        _registration = Catch.Exception(() => DatabaseNames.ForEventStore(new EventStoreName("min"), "ad"));
        _namespaceRegistration = Catch.Exception(() => DatabaseNames.ForEventStoreNamespace(new EventStoreName("min"), EventStoreNamespaceName.Default, "ad"));
        _other = DatabaseNames.ForReadModels(new EventStoreName("a"), new EventStoreNamespaceName("estate"));
    }

    [Fact] void should_reject_the_namespace() => _namespace.ShouldBeOfExactType<InvalidDatabaseName>();
    [Fact] void should_reject_the_namespace_in_any_casing() => _namespaceUpper.ShouldBeOfExactType<InvalidDatabaseName>();
    [Fact] void should_reject_the_namespace_with_a_prefix() => _prefixedNamespace.ShouldBeOfExactType<InvalidDatabaseName>();
    [Fact] void should_reject_registering_the_store_whose_prefix_makes_an_internal_name() => _registration.ShouldBeOfExactType<InvalidDatabaseName>();
    [Fact] void should_reject_the_namespace_database_of_such_a_store() => _namespaceRegistration.ShouldBeOfExactType<InvalidDatabaseName>();
    [Fact] void should_accept_other_namespaces() => _other.ShouldEqual("a+estate");
}
