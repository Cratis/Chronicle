// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.EventSequences.for_EventSequenceStorage.when_appending_many;

public class and_events_have_their_appended_generation : given.an_event_sequence_storage
{
    uint?[] _generations;

    async Task Because()
    {
        await AppendMany(EventSequenceNumber.First, new EventSequenceNumber(1));
        await using var context = CreateContext();
        _generations = context.Events.OrderBy(_ => _.SequenceNumber).Select(_ => _.Generation).ToArray();
    }

    [Fact] void should_store_the_generation_for_each_event() => _generations.ShouldEqual([EventTypeGeneration.First.Value, EventTypeGeneration.First.Value]);
}
