// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.EventSequences.for_EventEntryConverter.when_getting_the_read_generation;

public class and_the_revised_generation_has_object_content : Specification
{
    EventEntry _entry;
    uint _result;

    void Establish() => _entry = new EventEntry { Content = """{"1":{"a":1},"2":{"a":2},"3":{"a":3}}""", RevisedGeneration = 2 };

    void Because() => _result = EventEntryConverter.GetReadGeneration(_entry);

    [Fact] void should_return_the_revised_generation() => _result.ShouldEqual(2u);
}
