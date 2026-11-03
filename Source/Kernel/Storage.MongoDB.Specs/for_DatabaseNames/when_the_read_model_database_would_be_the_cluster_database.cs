// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;

namespace Cratis.Chronicle.Storage.MongoDB.for_DatabaseNames;

public class when_the_read_model_database_would_be_the_cluster_database : Specification
{
    Exception _unprefixed;
    Exception _prefixed;
    Exception _mixedCase;

    void Because()
    {
        _unprefixed = Catch.Exception(() => DatabaseNames.ForReadModels(new EventStoreName("chronicle"), new EventStoreNamespaceName("main")));
        _prefixed = Catch.Exception(() => DatabaseNames.ForReadModels(new EventStoreName("chronicle"), new EventStoreNamespaceName("main"), "run_"));
        _mixedCase = Catch.Exception(() => DatabaseNames.ForReadModels(new EventStoreName("Chronicle"), new EventStoreNamespaceName("MAIN"), "run_"));
    }

    [Fact] void should_refuse_without_a_prefix() => _unprefixed.ShouldBeOfExactType<InvalidDatabaseName>();
    [Fact] void should_refuse_with_a_prefix() => _prefixed.ShouldBeOfExactType<InvalidDatabaseName>();
    [Fact] void should_refuse_in_any_case() => _mixedCase.ShouldBeOfExactType<InvalidDatabaseName>();
}
