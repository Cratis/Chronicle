// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;

namespace Cratis.Chronicle.Storage.MongoDB.for_DatabaseNames;

/// <summary>
/// An event store named the same as the System event store's databases would share their databases.
/// </summary>
public class when_an_event_store_is_named_like_the_system_databases : Specification
{
    Exception _exception;

    void Because() => _exception = Catch.Exception(() => DatabaseNames.ForEventStore(new EventStoreName("_SYSTEM")));

    [Fact] void should_reject_the_name() => _exception.ShouldBeOfExactType<InvalidDatabaseName>();
    [Fact] void should_say_the_name_is_reserved() => _exception.Message.ShouldContain("reserved for the System event store");
}
