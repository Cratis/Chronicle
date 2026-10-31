// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Storage.EventSequences;
using Cratis.Monads;

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.EventSequences.for_EventSequenceStorage.when_appending_many_with_identities;

public class and_a_sequence_slot_collides : given.a_keyed_batch_storage
{
    Result<IEnumerable<AppendedEvent>, AppendManyStorageFailure> _result;
    async Task Establish() => (await _storage.AppendPublication(_publication, _event)).IsSuccess.ShouldBeTrue();
    async Task Because() => _result = await _storage.AppendManyWithIdentities([_event with { Identity = new EventPublication("new", "intent") }, _event with { SequenceNumber = 1 }]);
    [Fact] void should_report_the_next_available_slot() => _result.AsT1.AsT0.NextAvailableSequenceNumber.Value.ShouldEqual(1UL);
    [Fact] async Task should_not_write_any_batch_events() => (await _storage.GetTailSequenceNumber()).ShouldEqual(_event.SequenceNumber);
    [Fact] async Task should_not_write_the_new_identity() => (await _storage.TryGetPublications(["new"])).Count.ShouldEqual(0);
}
