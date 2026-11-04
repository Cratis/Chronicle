// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Compliance;
using Cratis.Chronicle.Storage.Naming;

namespace Cratis.Chronicle.Compliance.GDPR.for_PIIManager.when_deleting_an_encryption_key;

/// <summary>
/// The namespace the erasure is about is quarantined in a second event store whose other namespaces are fine. That
/// event store is not reached for the namespace, and the erasure says so by failing.
/// </summary>
public class and_a_namespace_is_quarantined_in_another_event_store : given.a_pii_manager
{
    Exception _error;

    void Establish() => _resourceNames.Publish(new ResourceNameAuditResult([], [new ResourceOwner(OtherEventStore.Value, EventStoreNamespace.Value)], []));

    async Task Because() => _error = await Catch.Exception(() => _manager.DeleteEncryptionKeyFor(Identifier));

    [Fact] void should_report_the_erasure_as_incomplete() => _error.ShouldBeOfExactType<EncryptionKeyErasureIncomplete>();
    [Fact] void should_say_which_namespace_was_left_out() => ((EncryptionKeyErasureIncomplete)_error).Failures.OfType<QuarantinedResourcesNotReached>().Single().Resources.ShouldContain($"{OtherEventStore.Value}/{EventStoreNamespace.Value}");
    [Fact] void should_erase_the_key_from_the_event_store_that_is_not_quarantined() => _keyStore.Received(1).DeleteFor(EventStore, EventStoreNamespace, Identifier);
    [Fact] void should_not_touch_the_quarantined_namespace() => _keyStore.DidNotReceive().DeleteFor(OtherEventStore, EventStoreNamespace, Identifier);
    [Fact] void should_not_log_the_erasure_as_done() => _logger.Messages.ShouldNotContain(_ => _.Contains("Erased the encryption key"));
}
