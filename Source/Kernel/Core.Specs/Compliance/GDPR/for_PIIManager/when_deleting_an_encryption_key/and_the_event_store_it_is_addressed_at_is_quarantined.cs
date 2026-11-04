// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Compliance;
using Cratis.Chronicle.Storage.Naming;
using NSubstitute.ExceptionExtensions;

namespace Cratis.Chronicle.Compliance.GDPR.for_PIIManager.when_deleting_an_encryption_key;

/// <summary>
/// The event store the caller named stays in the erasure even when it is quarantined, so that its refusal is reported
/// as the failure it is, while the other event stores are still erased.
/// </summary>
public class and_the_event_store_it_is_addressed_at_is_quarantined : given.a_pii_manager
{
    Exception _error;

    void Establish()
    {
        _resourceNames.Publish(new ResourceNameAuditResult([EventStore.Value], [], []));
        _keyStore.RecordErasureFor(EventStore, EventStoreNamespace, Identifier).ThrowsAsync(new ResourceQuarantined());
        _keyStore.DeleteFor(EventStore, EventStoreNamespace, Identifier).ThrowsAsync(new ResourceQuarantined());
    }

    async Task Because() => _error = await Catch.Exception(() => _manager.DeleteEncryptionKeyFor(Identifier));

    [Fact] void should_report_the_erasure_as_incomplete() => _error.ShouldBeOfExactType<EncryptionKeyErasureIncomplete>();
    [Fact] void should_try_the_event_store_it_was_asked_for() => _keyStore.Received(1).DeleteFor(EventStore, EventStoreNamespace, Identifier);
    [Fact] void should_still_erase_the_key_from_the_other_event_store() => _keyStore.Received(1).DeleteFor(OtherEventStore, EventStoreNamespace, Identifier);
    [Fact] void should_not_log_the_erasure_as_done() => _logger.Messages.ShouldNotContain(_ => _.Contains("Erased the encryption key"));
}
