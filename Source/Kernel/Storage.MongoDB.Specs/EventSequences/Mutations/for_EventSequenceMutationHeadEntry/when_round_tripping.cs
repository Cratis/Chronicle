// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.EventSequences;
using Cratis.Chronicle.Concepts.EventSequences.Mutations;
using Cratis.Chronicle.Storage.EventSequences.Mutations;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;

namespace Cratis.Chronicle.Storage.MongoDB.EventSequences.Mutations.for_EventSequenceMutationHeadEntry;

public class when_round_tripping : Specification
{
    EventSequenceMutationHeadEntry _entry;
    EventSequenceMutationHeadEntry _roundTripped;
    BsonDocument _document;

    void Establish() => _entry = new(
        EventSequenceId.Log,
        EventSequenceMutationCoverage.Sealed,
        new EventSequenceMutationOrdinal(42L),
        new EventSequenceMutation(
            new EventSequenceMutationId(Guid.NewGuid()),
            new EventSequenceMutationOrdinal(42L),
            new EventSequenceMutationOrigin(EventSequenceId.System, EventSequenceNumber.First),
            new EventSequenceMutationCommandEnvelope(EventSequenceMutationKind.Revision, "payload", "command-hash"),
            new EventSequenceMutationTarget(EventSequenceNumber.First, new EventSequenceNumber(2UL), 1),
            EventSequenceMutationPhase.Applying,
            EventSequenceMutationPhase.None,
            EventSequenceMutationRepairState.Pending));

    void Because()
    {
        _document = _entry.ToBsonDocument();
        _roundTripped = BsonSerializer.Deserialize<EventSequenceMutationHeadEntry>(_document);
    }

    [Fact] void should_round_trip_the_entry() => _roundTripped.ShouldEqual(_entry);
    [Fact] void should_map_the_event_sequence_id_to_the_mongodb_id() => _document["_id"].AsString.ShouldEqual(EventSequenceId.Log.Value);
    [Fact] void should_use_camel_case_for_coverage() => _document.Contains("coverage").ShouldBeTrue();
    [Fact] void should_use_camel_case_for_the_last_assigned_ordinal() => _document.Contains("lastAssignedOrdinal").ShouldBeTrue();
    [Fact] void should_use_camel_case_for_the_active_mutation() => _document.Contains("active").ShouldBeTrue();
    [Fact] void should_store_the_last_assigned_ordinal_as_int64() => _document["lastAssignedOrdinal"].IsInt64.ShouldBeTrue();
    [Fact] void should_not_write_a_discriminator() => _document.Contains("_t").ShouldBeFalse();
    [Fact] void should_not_write_an_active_mutation_discriminator() => _document["active"].AsBsonDocument.Contains("_t").ShouldBeFalse();
}
