// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.EventSequences;
using Cratis.Chronicle.Concepts.EventSequences.Mutations;
using Cratis.Chronicle.Storage.EventSequences.Mutations;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;

namespace Cratis.Chronicle.Storage.MongoDB.EventSequences.Mutations.for_EventSequenceMutationHistoryEntry;

public class when_round_tripping : Specification
{
    EventSequenceMutationHistoryEntry _entry;
    EventSequenceMutationHistoryEntry _roundTripped;
    BsonDocument _document;

    void Establish()
    {
        var definitionDigest = new EventSequenceMutationDefinitionDigestV1(new byte[32]);
        var receiptDigest = new EventSequenceMutationReceiptDigestV1(Enumerable.Repeat((byte)1, 32).ToArray());
        _entry = new(
            new EventSequenceMutationId(Guid.NewGuid()),
            EventSequenceId.Log,
            new EventSequenceMutationOrdinal(42L),
            new EventSequenceMutationOrigin(EventSequenceMutationIdentity.TryCreate(EventSequenceId.System.Value).Identity!, EventSequenceNumber.First),
            EventSequenceMutationKind.Revision,
            new EventSequenceMutationCommandHash("command-hash"),
            new EventSequenceMutationTarget(EventSequenceNumber.First, new EventSequenceNumber(1UL), 1),
            EventSequenceMutationRepairState.Accepted,
            new(EventSequenceMutationStateVersion.First, definitionDigest, receiptDigest));
    }

    void Because()
    {
        _document = _entry.ToBsonDocument();
        _roundTripped = BsonSerializer.Deserialize<EventSequenceMutationHistoryEntry>(_document);
    }

    [Fact] void should_round_trip_the_entry() => _roundTripped.ShouldEqual(_entry);
    [Fact] void should_map_the_mutation_id_to_the_mongodb_id() => _document["_id"].AsGuid.ShouldEqual(_entry.MutationId.Value);
    [Fact] void should_use_camel_case_for_the_event_sequence_id() => _document.Contains("eventSequenceId").ShouldBeTrue();
    [Fact] void should_use_camel_case_for_the_ordinal() => _document.Contains("ordinal").ShouldBeTrue();
    [Fact] void should_use_camel_case_for_the_origin() => _document.Contains("origin").ShouldBeTrue();
    [Fact] void should_use_camel_case_for_the_kind() => _document.Contains("kind").ShouldBeTrue();
    [Fact] void should_use_camel_case_for_the_command_hash() => _document.Contains("commandHash").ShouldBeTrue();
    [Fact] void should_use_camel_case_for_the_target() => _document.Contains("target").ShouldBeTrue();
    [Fact] void should_use_camel_case_for_the_repair_state() => _document.Contains("repairState").ShouldBeTrue();
    [Fact] void should_use_camel_case_for_the_terminal_witness() => _document.Contains("terminalWitness").ShouldBeTrue();
    [Fact] void should_store_the_ordinal_as_int64() => _document["ordinal"].IsInt64.ShouldBeTrue();
    [Fact] void should_not_write_a_discriminator() => _document.Contains("_t").ShouldBeFalse();
    [Fact] void should_not_persist_a_command_payload() => _document.ToJson().Contains("\"payload\"", StringComparison.Ordinal).ShouldBeFalse();
}
