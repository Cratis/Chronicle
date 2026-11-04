// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Compliance;
using Cratis.Chronicle.Storage.Naming;

namespace Cratis.Chronicle.Compliance.GDPR.for_PIIManager.when_deleting_an_encryption_key;

/// <summary>
/// A quarantined event store cannot be opened. It must not stop the event stores that can be reached from being
/// erased, but an erasure that left a store alone has not erased the subject: it fails, and never reports success.
/// </summary>
public class and_an_event_store_is_quarantined : given.a_pii_manager
{
    Exception _error;

    void Establish() => _resourceNames.Publish(new ResourceNameAuditResult([OtherEventStore.Value], [], []));

    async Task Because() => _error = await Catch.Exception(() => _manager.DeleteEncryptionKeyFor(Identifier));

    [Fact] void should_report_the_erasure_as_incomplete() => _error.ShouldBeOfExactType<EncryptionKeyErasureIncomplete>();
    [Fact] void should_say_which_event_store_was_left_out() => ((EncryptionKeyErasureIncomplete)_error).Failures.OfType<QuarantinedResourcesNotReached>().Single().Resources.ShouldContain(OtherEventStore.Value);
    [Fact] void should_erase_the_key_from_the_event_store_that_is_not_quarantined() => _keyStore.Received(1).DeleteFor(EventStore, EventStoreNamespace, Identifier);
    [Fact] void should_not_touch_the_quarantined_event_store() => _keyStore.DidNotReceive().DeleteFor(OtherEventStore, EventStoreNamespace, Identifier);
    [Fact] void should_not_fence_the_quarantined_event_store() => _keyStore.DidNotReceive().RecordErasureFor(OtherEventStore, EventStoreNamespace, Identifier);
    [Fact] void should_report_the_event_store_that_was_left_out() => _logger.Messages.ShouldContain(_ => _.Contains(OtherEventStore.Value) && _.Contains("quarantined"));
    [Fact] void should_not_log_the_erasure_as_done() => _logger.Messages.ShouldNotContain(_ => _.Contains("Erased the encryption key"));
}
