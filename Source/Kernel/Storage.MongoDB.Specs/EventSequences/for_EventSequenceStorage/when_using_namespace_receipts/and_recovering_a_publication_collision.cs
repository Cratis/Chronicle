// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.EventSequences;

namespace Cratis.Chronicle.Storage.MongoDB.EventSequences.for_EventSequenceStorage.when_using_namespace_receipts;

public class and_recovering_a_publication_collision : given.a_fenced_receipt_storage
{
    EventPublicationReceipt _outcome;

    void Establish()
    {
        _writeFailure = Duplicate();
        _duringWrite = StorePublication;
    }

    async Task Because() => _outcome = (EventPublicationReceipt)await _storage.AppendPublication(_context, _publication, _event);

    [Fact] void should_return_the_original_slot() => _outcome.SequenceNumber.Value.ShouldEqual(99UL);
    [Fact] void should_refence_before_settling_the_collision() => _steps.SequenceEqual(["fence", "insert", "abort", "fence", "commit"]).ShouldBeTrue();
    [Fact] void should_lookup_on_the_recovery_session() => _lookupSessions[^1].ShouldEqual(_sessions[1]);
}
