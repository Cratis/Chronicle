// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.EventSequences;

namespace Cratis.Chronicle.Storage.MongoDB.EventSequences.for_EventSequenceStorage.when_using_namespace_receipts;

public class and_publishing_an_event : given.a_fenced_receipt_storage
{
    EventPublicationReceipt _outcome;

    async Task Because() => _outcome = (EventPublicationReceipt)await _storage.AppendPublication(_context, _publication, _event);

    [Fact] void should_return_the_appended_slot() => _outcome.SequenceNumber.ShouldEqual(_event.SequenceNumber);
    [Fact] void should_commit_after_the_fence_and_write() => _steps.SequenceEqual(["fence", "insert", "commit"]).ShouldBeTrue();
    [Fact] void should_read_on_the_fenced_session() => _lookupSessions.Single().ShouldEqual(_sessions.Single());
    [Fact] void should_write_on_the_same_session() => _writeSessions.Single().ShouldEqual(_sessions.Single());
    [Fact] void should_use_the_explicit_publication_identity() => _inserted.Single().PublicationId.ShouldEqual(_publication.Id);
    [Fact] void should_preserve_the_fingerprint() => _inserted.Single().PublicationFingerprint.ShouldEqual(_publication.Fingerprint);
}
