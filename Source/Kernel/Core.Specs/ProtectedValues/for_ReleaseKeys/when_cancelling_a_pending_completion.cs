// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Storage.Compliance;

namespace Cratis.Chronicle.ProtectedValues.for_ReleaseKeys;

public class when_cancelling_a_pending_completion : given.a_key_store
{
    readonly given.a_release_clock _clock = new();
    readonly TaskCompletionSource<EncryptionKeyErasure?> _pending = new();
    Exception _error;
    bool _handedOff;

    async Task Because()
    {
        using var scope = ReleaseKeys.BeginScope(_clock);
        await Release(_keys);
        _clock.Advance(TimeSpan.FromSeconds(3));
        _keys.GetErasureForRelease(EventStoreName.NotSet, EventStoreNamespaceName.NotSet, Identifier).Returns(_pending.Task);
        using var cancellation = new CancellationTokenSource();
        var completion = ReleaseKeys.Complete(() => _handedOff = true, cancellation.Token);
        await cancellation.CancelAsync();
        _error = await Catch.Exception(() => completion);
    }

    [Fact] void should_cancel_the_refresh() => (_error is OperationCanceledException).ShouldBeTrue();
    [Fact] void should_not_hand_off_protected_material() => _handedOff.ShouldBeFalse();
    [Fact] void should_not_require_the_provider_to_finish() => _pending.Task.IsCompleted.ShouldBeFalse();
}
