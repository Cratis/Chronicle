// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;

namespace Cratis.Chronicle.Storage.MongoDB.for_DatabaseNames;

/// <summary>
/// A tenant id such as "Hive Consulting" reaches storage as the namespace. Left alone it composes a database
/// name MongoDB rejects, and the driver's bare "Invalid namespace specified" names neither the event store nor
/// the namespace that produced it.
/// </summary>
public class when_the_namespace_contains_a_space : Specification
{
    Exception _exception;

    void Because() => _exception = Catch.Exception(() => DatabaseNames.ForReadModels(new EventStoreName("Ada"), new EventStoreNamespaceName("Hive Consulting")));

    [Fact] void should_reject_the_composed_name() => _exception.ShouldBeOfExactType<InvalidDatabaseName>();

    [Fact] void should_say_the_space_is_the_problem() => _exception.Message.ShouldContain("a space");

    [Fact] void should_name_the_offending_namespace() => _exception.Message.ShouldContain("Hive Consulting");

    [Fact] void should_name_the_event_store() => _exception.Message.ShouldContain("Ada");
}
