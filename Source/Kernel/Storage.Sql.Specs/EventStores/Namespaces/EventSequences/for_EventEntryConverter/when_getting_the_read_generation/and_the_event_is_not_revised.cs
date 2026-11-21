// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.EventSequences.for_EventEntryConverter.when_getting_the_read_generation;

public class and_the_event_is_not_revised : Specification
{
    EventEntry _entry;
    uint _result;

    void Establish() => _entry = new EventEntry { Content = """{"1":{"a":1},"4":{"a":4},"2":{"a":2}}""" };

    void Because() => _result = EventEntryConverter.GetReadGeneration(_entry);

    [Fact] void should_return_the_highest_generation() => _result.ShouldEqual(4u);
}
