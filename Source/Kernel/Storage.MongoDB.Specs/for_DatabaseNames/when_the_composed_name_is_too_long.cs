// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;

namespace Cratis.Chronicle.Storage.MongoDB.for_DatabaseNames;

public class when_the_composed_name_is_too_long : Specification
{
    Exception _exception;

    void Because() => _exception = Catch.Exception(() => DatabaseNames.ForEventStoreNamespace(
        new EventStoreName(new string('a', 40)),
        new EventStoreNamespaceName(new string('b', 40))));

    [Fact] void should_reject_the_composed_name() => _exception.ShouldBeOfExactType<InvalidDatabaseName>();

    [Fact] void should_say_how_long_mongodb_allows() => _exception.Message.ShouldContain("at most 63");
}
