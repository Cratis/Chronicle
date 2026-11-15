// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Storage.Compliance;

namespace Cratis.Chronicle.ProtectedValues.for_ReleaseKeys;

public class when_cancelling_a_pending_release : given.a_key_store
{
    readonly TaskCompletionSource<EncryptionKeyErasure?> _pending = new();
    Exception _error;

    void Establish() => _keys.GetErasureForRelease(EventStoreName.NotSet, EventStoreNamespaceName.NotSet, Identifier).Returns(_pending.Task);

    async Task Because()
    {
        using var scope = ReleaseKeys.BeginScope();
        using var cancellation = new CancellationTokenSource();
        var release = ReleaseKeys.TryGetForRelease(_keys, EventStoreName.NotSet, EventStoreNamespaceName.NotSet, Identifier, cancellation.Token);
        await cancellation.CancelAsync();
        _error = await Catch.Exception(() => release);
    }

    [Fact] void should_honour_cancellation_without_waiting_for_the_store() => (_error is OperationCanceledException).ShouldBeTrue();
    [Fact] void should_not_require_the_provider_to_finish() => _pending.Task.IsCompleted.ShouldBeFalse();
    [Fact] void should_not_retry_after_cancellation() => _keys.Received(1).GetErasureForRelease(EventStoreName.NotSet, EventStoreNamespaceName.NotSet, Identifier);
}
