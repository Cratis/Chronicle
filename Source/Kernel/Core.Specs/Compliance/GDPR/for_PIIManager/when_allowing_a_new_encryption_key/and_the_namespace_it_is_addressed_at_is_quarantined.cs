// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Compliance;
using Cratis.Chronicle.Storage.Naming;

namespace Cratis.Chronicle.Compliance.GDPR.for_PIIManager.when_allowing_a_new_encryption_key;

/// <summary>
/// The namespace the manager is addressed at is quarantined in the event store it is addressed at. The event store is
/// still tried, but the namespace was not reached, so the authorization fails instead of reporting success.
/// </summary>
public class and_the_namespace_it_is_addressed_at_is_quarantined : given.a_pii_manager
{
    Exception _error;

    void Establish() => _resourceNames.Publish(new ResourceNameAuditResult([], [new ResourceOwner(EventStore.Value, EventStoreNamespace.Value)], []));

    async Task Because() => _error = await Catch.Exception(() => _manager.AllowNewEncryptionKeyFor(Identifier));

    [Fact] void should_report_the_authorization_as_incomplete() => _error.ShouldBeOfExactType<EncryptionKeyLifecycleIncomplete>();
    [Fact] void should_say_which_namespace_was_not_reached() => ((EncryptionKeyLifecycleIncomplete)_error).Failures.OfType<QuarantinedResourcesNotReached>().Single().Resources.ShouldContain($"{EventStore.Value}/{EventStoreNamespace.Value}");
    [Fact] void should_try_the_event_store_it_was_asked_for() => _keyStore.Received(1).AllowNewKeyFor(EventStore, EventStoreNamespace, Identifier);
    [Fact] void should_not_log_the_authorization_as_done() => _logger.Messages.ShouldNotContain(_ => _.Contains("Authorized a new encryption key"));
}
