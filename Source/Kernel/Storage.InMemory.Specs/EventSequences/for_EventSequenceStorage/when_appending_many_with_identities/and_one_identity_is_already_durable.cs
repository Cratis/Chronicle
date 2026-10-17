// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Storage.EventSequences;
using Cratis.Monads;

namespace Cratis.Chronicle.Storage.InMemory.EventSequences.for_EventSequenceStorage.when_appending_many_with_identities;

public class and_one_identity_is_already_durable : given.a_keyed_batch_storage
{
    Result<IEnumerable<AppendedEvent>, AppendManyStorageFailure> _result;
    async Task Establish() => (await _storage.AppendPublication(_publication, _event)).IsSuccess.ShouldBeTrue();
    async Task Because() => _result = await _storage.AppendManyWithIdentities([_event with { SequenceNumber = 1, Identity = new EventPublication("new", "new-intent") }, _event with { SequenceNumber = 2, Identity = _publication }]);
    [Fact] void should_report_the_existing_receipt() => _result.AsT1.AsT1.Receipts[_publication.Id].SequenceNumber.ShouldEqual(_event.SequenceNumber);
    [Fact] async Task should_not_write_any_batch_events() => (await _storage.GetTailSequenceNumber()).ShouldEqual(_event.SequenceNumber);
    [Fact] async Task should_not_write_the_new_identity() => (await _storage.TryGetPublications(["new"])).Count.ShouldEqual(0);
}
