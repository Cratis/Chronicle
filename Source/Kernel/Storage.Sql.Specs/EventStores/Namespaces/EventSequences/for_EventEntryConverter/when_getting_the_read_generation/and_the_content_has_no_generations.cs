// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.EventSequences.for_EventEntryConverter.when_getting_the_read_generation;

public class and_the_content_has_no_generations : Specification
{
    EventEntry _entry;
    uint _result;

    void Establish() => _entry = new EventEntry { Content = "{}" };

    void Because() => _result = EventEntryConverter.GetReadGeneration(_entry);

    [Fact] void should_return_first_generation() => _result.ShouldEqual(1u);
}
