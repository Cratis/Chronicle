// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.EventSequences;

namespace Cratis.Chronicle.Storage.MongoDB.EventSequences.for_EventSequenceStorage.when_using_namespace_receipts;

public class and_replaying_a_publication : given.a_fenced_receipt_storage
{
    EventPublicationReceipt _outcome;

    void Establish() => StorePublication();
    async Task Because() => _outcome = (EventPublicationReceipt)await _storage.AppendPublication(_context, _publication, _event);

    [Fact] void should_return_the_original_slot() => _outcome.SequenceNumber.Value.ShouldEqual(99UL);
    [Fact] void should_commit_the_fence_without_another_event() => _steps.SequenceEqual(["fence", "commit"]).ShouldBeTrue();
    [Fact] void should_lookup_on_the_fenced_session() => _lookupSessions.Single().ShouldEqual(_sessions.Single());
    [Fact] void should_write_no_events() => _writeSessions.ShouldBeEmpty();
}
