// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Storage.EventSequences;
using Cratis.Monads;

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.EventSequences.for_EventSequenceStorage.when_publishing;

public class and_a_new_storage_instance_retries_after_a_restart : given.a_publication_storage
{
    Result<EventPublicationReceipt, DuplicateEventSequenceNumber> _result;
    EventCount _count;

    async Task Establish() => (await _storage.AppendPublication(_publication, _event)).IsSuccess.ShouldBeTrue();

    async Task Because()
    {
        var restarted = RecreateStorage();
        _result = await restarted.AppendPublication(_publication, _event with { SequenceNumber = 7 });
        _count = await restarted.GetCount();
    }

    [Fact] void should_find_the_durable_publication() => _result.AsT0.SequenceNumber.ShouldEqual(EventSequenceNumber.First);
    [Fact] void should_not_duplicate_it() => _count.Value.ShouldEqual(1UL);
}
