// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Naming;

namespace Cratis.Chronicle.Compliance.GDPR.for_PIIManager.when_deleting_an_encryption_key;

/// <summary>
/// A quarantined event store cannot be opened. It must not make every erasure fail for as long as it is quarantined,
/// but the erasure must say plainly which event stores it left alone.
/// </summary>
public class and_an_event_store_is_quarantined : given.a_pii_manager
{
    Exception _error;

    void Establish() => _resourceNames.Publish(new ResourceNameAuditResult([OtherEventStore.Value], [], []));

    async Task Because() => _error = await Catch.Exception(() => _manager.DeleteEncryptionKeyFor(Identifier));

    [Fact] void should_complete_the_erasure() => _error.ShouldBeNull();
    [Fact] void should_erase_the_key_from_the_event_store_that_is_not_quarantined() => _keyStore.Received(1).DeleteFor(EventStore, EventStoreNamespace, Identifier);
    [Fact] void should_not_touch_the_quarantined_event_store() => _keyStore.DidNotReceive().DeleteFor(OtherEventStore, EventStoreNamespace, Identifier);
    [Fact] void should_not_fence_the_quarantined_event_store() => _keyStore.DidNotReceive().RecordErasureFor(OtherEventStore, EventStoreNamespace, Identifier);
    [Fact] void should_report_the_event_store_that_was_left_out() => _logger.Messages.ShouldContain(_ => _.Contains(OtherEventStore.Value) && _.Contains("quarantined"));
}
