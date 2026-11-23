// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.EventSequences;

namespace Cratis.Chronicle.Storage.MongoDB.EventSequences.for_EventSequenceStorage.when_using_namespace_receipts;

public class and_a_publication_fingerprint_differs : given.a_fenced_receipt_storage
{
    Exception _readFailure;

    void Establish()
    {
        StorePublication();
        _publication = _publication with { Fingerprint = "different" };
    }

    async Task Because() => _readFailure = await Catch.Exception(() => _storage.TryGetPublication(_context, _publication));

    [Fact] void should_reject_the_reused_identity() => _readFailure.ShouldBeOfExactType<EventPublicationConflict>();
    [Fact] void should_validate_after_the_fence() => _lookupSessions.Single().ShouldEqual(_sessions.Single());
    [Fact] void should_abort_the_transaction() => _steps.SequenceEqual(["fence", "abort"]).ShouldBeTrue();
}
