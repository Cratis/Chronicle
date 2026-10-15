// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Storage.EventSequences;

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.EventSequences.for_EventSequenceStorage.when_publishing;

public class and_reusing_an_identity_for_a_different_intent : given.a_publication_storage
{
    Exception? _error;
    EventCount _count;

    async Task Establish() => (await _storage.AppendPublication(_publication, _event)).IsSuccess.ShouldBeTrue();

    async Task Because()
    {
        _error = await Specifications.Catch.Exception(() => _storage.AppendPublication(_publication with { Fingerprint = "different" }, _event with { SequenceNumber = 1 }));
        _count = await _storage.GetCount();
    }

    [Fact] void should_fail_closed() => _error.ShouldBeOfExactType<EventPublicationConflict>();
    [Fact] void should_not_append_another_event() => _count.Value.ShouldEqual(1UL);
}
