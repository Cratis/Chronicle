// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Storage.Compliance;
using NSubstitute.ExceptionExtensions;

namespace Cratis.Chronicle.Compliance.GDPR.for_PIIManager.when_deleting_an_encryption_key;

/// <summary>
/// Reaching the stores together must not change what a failing store does to the erasure: every other store is still
/// swept and evicted, nothing is confirmed, no grace is waited and the erasure reports itself incomplete.
/// </summary>
public class and_one_of_many_event_stores_cannot_be_swept : given.many_event_stores
{
    static readonly EventStoreName _failing = EventStores[NumberOfEventStores / 2];
    Exception _error;

    void Establish() =>
        _keyStore
            .DeleteThroughFor(_failing, EventStoreNamespace, Identifier, Arg.Any<string>())
            .ThrowsAsync(new StoreUnreachable());

    async Task Because() => _error = await Catch.Exception(() => _manager.DeleteEncryptionKeyFor(Identifier));

    [Fact] void should_fence_every_event_store() => Reached(Fence).ShouldContainOnly(EventStores);
    [Fact] void should_sweep_every_other_event_store() => Reached(Sweep).ShouldContainOnly(EventStores.Where(_ => _ != _failing));
    [Fact] void should_evict_from_every_event_store() => Reached(Evict).ShouldContainOnly(EventStores);
    [Fact] void should_never_confirm() => Reached(Confirm).ShouldBeEmpty();
    [Fact] void should_never_compact() => Reached(Compact).ShouldBeEmpty();
    [Fact] void should_not_wait_the_revocation_grace() => _delays.ShouldBeEmpty();
    [Fact] void should_report_the_erasure_as_incomplete() => _error.ShouldBeOfExactType<EncryptionKeyErasureIncomplete>();
    [Fact] void should_report_only_the_failing_store() => ((EncryptionKeyErasureIncomplete)_error).Failures.Count.ShouldEqual(1);
}
