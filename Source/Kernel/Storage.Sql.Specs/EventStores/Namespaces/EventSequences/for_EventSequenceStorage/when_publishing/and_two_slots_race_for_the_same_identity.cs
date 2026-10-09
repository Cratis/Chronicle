// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Storage.EventSequences;
using Cratis.Monads;

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.EventSequences.for_EventSequenceStorage.when_publishing;

public class and_two_slots_race_for_the_same_identity : given.a_publication_storage
{
    Result<EventPublicationReceipt, DuplicateEventSequenceNumber>[] _results;
    EventCount _count;

    async Task Because()
    {
        _results = await Task.WhenAll(
            Task.Run(() => _storage.AppendPublication(_publication, _event)),
            Task.Run(() => _storage.AppendPublication(_publication, _event with { SequenceNumber = 1 })));
        _count = await _storage.GetCount();
    }

    [Fact] void should_commit_exactly_one_event() => _count.Value.ShouldEqual(1UL);
    [Fact] void should_return_success_to_both_callers() => _results.All(_ => _.IsSuccess).ShouldBeTrue();
    [Fact] void should_return_the_same_durable_slot() => _results.Select(_ => _.AsT0.SequenceNumber).Distinct().Count().ShouldEqual(1);
}
