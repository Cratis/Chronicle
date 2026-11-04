// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Compliance;
using Cratis.Chronicle.Storage.Naming;

namespace Cratis.Chronicle.Compliance.GDPR.for_PIIManager.when_allowing_a_new_encryption_key;

/// <summary>
/// An authorization that left a quarantined event store out has not reached it, so it fails instead of reporting
/// success, while the event stores that can be reached are still authorized.
/// </summary>
public class and_an_event_store_is_quarantined : given.a_pii_manager
{
    Exception _error;

    void Establish() => _resourceNames.Publish(new ResourceNameAuditResult([OtherEventStore.Value], [], []));

    async Task Because() => _error = await Catch.Exception(() => _manager.AllowNewEncryptionKeyFor(Identifier));

    [Fact] void should_report_the_authorization_as_incomplete() => _error.ShouldBeOfExactType<EncryptionKeyLifecycleIncomplete>();
    [Fact] void should_authorize_the_event_store_that_is_not_quarantined() => _keyStore.Received(1).AllowNewKeyFor(EventStore, EventStoreNamespace, Identifier);
    [Fact] void should_not_touch_the_quarantined_event_store() => _keyStore.DidNotReceive().AllowNewKeyFor(OtherEventStore, EventStoreNamespace, Identifier);
    [Fact] void should_not_log_the_authorization_as_done() => _logger.Messages.ShouldNotContain(_ => _.Contains("Authorized a new encryption key"));
}
