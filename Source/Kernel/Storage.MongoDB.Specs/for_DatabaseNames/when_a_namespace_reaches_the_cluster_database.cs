// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;

namespace Cratis.Chronicle.Storage.MongoDB.for_DatabaseNames;

/// <summary>
/// A namespace cannot be used to compose the cluster database name out of an ordinary event store name.
/// </summary>
public class when_a_namespace_reaches_the_cluster_database : Specification
{
    Exception _unprefixed;
    Exception _prefixed;
    string _other;

    void Because()
    {
        _unprefixed = Catch.Exception(() => DatabaseNames.ForReadModels(new EventStoreName("chronicle"), new EventStoreNamespaceName("main")));
        _prefixed = Catch.Exception(() => DatabaseNames.ForReadModels(new EventStoreName("chronicle"), new EventStoreNamespaceName("main"), "run_"));
        _other = DatabaseNames.ForReadModels(new EventStoreName("chronicle"), new EventStoreNamespaceName("other"), "run_");
    }

    [Fact] void should_reject_it_without_a_prefix() => _unprefixed.ShouldBeOfExactType<InvalidDatabaseName>();
    [Fact] void should_reject_it_with_a_prefix() => _prefixed.ShouldBeOfExactType<InvalidDatabaseName>();
    [Fact] void should_accept_other_namespaces() => _other.ShouldEqual("run_chronicle+other");
}
