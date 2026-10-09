// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Storage.EventSequences;
using Cratis.Monads;

namespace Cratis.Chronicle.Storage.MongoDB.EventSequences.for_EventSequenceStorage.when_publishing;

[Collection(ReplicaSetMongoDBCollection.Name)]
public class and_a_different_publication_occupies_the_slot(ReplicaSetMongoDBFixture fixture) : a_publication(fixture)
{
    Result<EventPublicationReceipt, DuplicateEventSequenceNumber> _result;
    Option<EventPublicationReceipt> _receipt;

    async Task Establish() => (await _storage.AppendPublication(_publication with { Id = "other" }, PublicationEvent(EventSequenceNumber.First))).IsSuccess.ShouldBeTrue();

    async Task Because()
    {
        _result = await _storage.AppendPublication(_publication, PublicationEvent(EventSequenceNumber.First));
        _receipt = await _storage.TryGetPublication(_publication);
    }

    [Fact] void should_report_a_slot_collision_not_success() => _result.IsSuccess.ShouldBeFalse();
    [Fact] void should_report_the_next_available_slot() => _result.AsT1.NextAvailableSequenceNumber.ShouldEqual((EventSequenceNumber)1);
    [Fact] void should_not_record_a_publication_receipt() => _receipt.HasValue.ShouldBeFalse();
}
