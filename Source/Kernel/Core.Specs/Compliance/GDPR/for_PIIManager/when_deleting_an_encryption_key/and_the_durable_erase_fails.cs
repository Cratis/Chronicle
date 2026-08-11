// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Compliance;
using NSubstitute.ExceptionExtensions;

namespace Cratis.Chronicle.Compliance.GDPR.for_PIIManager.when_deleting_an_encryption_key;

/// <summary>
/// A composite key store attempts every inner store and then reports a partial failure, so the key can be destroyed in
/// the store that held it and the erasure still throw - and a transient outage throws the same way. Skipping the
/// cluster-wide eviction on a throw leaves every peer silo serving the key from a cache with no time-to-live, which
/// nothing clears short of a restart. The eviction is idempotent, so it is never gated on the erase succeeding.
/// </summary>
public class and_the_durable_erase_fails : given.a_pii_manager
{
    Exception _error;

    void Establish() =>
        _keyStore
            .DeleteFor(EventStore, EventStoreNamespace, Identifier)
            .ThrowsAsync(new EncryptionKeyErasureIncomplete(Identifier, [new StoreUnreachable()]));

    async Task Because() => _error = await Catch.Exception(() => _manager.DeleteEncryptionKeyFor(Identifier));

    [Fact] void should_still_evict_the_key_from_every_silo() => _cacheClient.Received(1).Evict(EventStore, EventStoreNamespace, Identifier);
    [Fact] void should_report_the_incomplete_erasure_to_the_caller() => _error.ShouldBeOfExactType<EncryptionKeyErasureIncomplete>();
}
