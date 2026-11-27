// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Storage.EventSequences;
using Cratis.Monads;

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.EventSequences.for_EventSequenceStorage.when_using_a_namespace_fence;

public class and_settling_publication_collisions : given.a_fenced_storage
{
    Result<IEnumerable<AppendedEvent>, AppendManyStorageFailure> _batch;
    Result<EventPublicationReceipt, DuplicateEventSequenceNumber> _replay;
    Exception _conflict;

    async Task Establish() => (await _fenced.AppendManyWithIdentities(_context, [_event])).IsSuccess.ShouldBeTrue();

    async Task Because()
    {
        _batch = await _fenced.AppendManyWithIdentities(_context, [_event with { SequenceNumber = 7 }]);
        _replay = await _fenced.AppendPublication(_context, _publication, _event with { SequenceNumber = 9 });
        _conflict = await Cratis.Specifications.Catch.Exception(() => _fenced.TryGetPublication(_context, _publication with { Fingerprint = "changed" }));
    }

    [Fact] void should_return_the_durable_identity() => _batch.AsT1.AsT1.Receipts[_publication.Id].SequenceNumber.ShouldEqual(EventSequenceNumber.First);
    [Fact] void should_return_the_original_publication_slot() => _replay.AsT0.SequenceNumber.ShouldEqual(EventSequenceNumber.First);
    [Fact] void should_reject_changed_fingerprints() => _conflict.ShouldBeOfExactType<EventPublicationConflict>();
    [Fact] async Task should_not_append_retries() => (await _storage.GetCount()).Value.ShouldEqual(1UL);
}
