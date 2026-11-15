// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;

namespace Cratis.Chronicle.Storage.MongoDB.for_DatabaseNames;

/// <summary>
/// A prefix cannot be used to compose a database-internal name out of an ordinary event store name.
/// </summary>
public class when_a_prefix_makes_a_database_name_reserved : Specification
{
    Exception _exception;

    void Because() => _exception = Catch.Exception(() => DatabaseNames.ForReadModels(new EventStoreName("min"), EventStoreNamespaceName.Default, "ad"));

    [Fact] void should_reject_the_name() => _exception.ShouldBeOfExactType<InvalidDatabaseName>();
}
