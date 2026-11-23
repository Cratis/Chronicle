// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.EventSequences.for_EventSequenceStorage.when_getting_state;

public class and_the_state_is_fully_populated : given.a_state_storage
{
    Chronicle.Storage.EventSequences.EventSequenceState _first;
    Chronicle.Storage.EventSequences.EventSequenceState _second;

    async Task Establish()
    {
        await _storage.SaveState(new()
        {
            SequenceNumber = 10,
            ConstraintIndexWatermark = 8,
            ConstraintIndexCatchUpBelow = 9,
            ConstraintIndexCatchUpRefusing = [new("unique-name")],
            MutationEpoch = 7,
            TailSequenceNumberPerEventType = new Dictionary<EventTypeId, EventSequenceNumber> { [new("first-type")] = 3, [new("second-type")] = 5 }
        });
        await using var scope = await _database.Namespace(_eventStore, _namespace);
        scope.DbContext.EventSequences.Add(new EventSequenceState { EventSequenceId = "another-sequence", SequenceNumber = 99 });
        await scope.DbContext.SaveChangesAsync();
    }

    async Task Because()
    {
        _first = await _storage.GetState();
        _first.TailSequenceNumberPerEventType.Clear();
        _second = await _storage.GetState();
    }

    [Fact] void should_read_the_sequence_number_of_this_sequence_only() => _second.SequenceNumber.Value.ShouldEqual(10UL);
    [Fact] void should_read_the_watermark() => _second.ConstraintIndexWatermark.Value.ShouldEqual(8UL);
    [Fact] void should_read_the_boundary() => _second.ConstraintIndexCatchUpBelow.Value.ShouldEqual(9UL);
    [Fact] void should_read_the_refusal() => _second.ConstraintIndexCatchUpRefusing.Single().Value.ShouldEqual("unique-name");
    [Fact] void should_read_the_mutation_epoch() => _second.MutationEpoch.Value.ShouldEqual(7UL);
    [Fact] void should_read_every_tail() => _second.TailSequenceNumberPerEventType.Count.ShouldEqual(2);
    [Fact] void should_read_a_tail_value() => _second.TailSequenceNumberPerEventType[new("second-type")].Value.ShouldEqual(5UL);
}
