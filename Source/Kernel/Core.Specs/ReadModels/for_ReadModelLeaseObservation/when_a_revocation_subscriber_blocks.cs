// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.ReadModels.for_ReadModelLeaseObservation;

public class when_a_revocation_subscriber_blocks : for_SinkProvenanceCache.given.a_cached_database
{
    readonly TaskCompletionSource _entered = new(TaskCreationOptions.RunContinuationsAsynchronously);
    readonly TaskCompletionSource _left = new(TaskCreationOptions.RunContinuationsAsynchronously);
    readonly ManualResetEventSlim _release = new();
    Exception? _error;
    bool _marked;
    bool _delivered;

    async Task Because()
    {
        await using var subscriber = _lease.Revoked.Register(() =>
        {
            _entered.TrySetResult();
            _release.Wait();
            _left.TrySetResult();
        });
        var marking = Task.Run(_cached.MarkQuarantined);
        try
        {
            await _entered.Task.WaitAsync(TimeSpan.FromSeconds(5), TimeProvider.System);
            _error = await Catch.Exception(() => marking.WaitAsync(TimeSpan.FromSeconds(5), TimeProvider.System));
            _marked = await _raw.IsQuarantined();
            _ = Catch.Exception(() => _lease.Deliver(() => _delivered = true));
        }
        finally
        {
            _release.Set();
            await marking;
            await _left.Task.WaitAsync(TimeSpan.FromSeconds(5), TimeProvider.System);
            _release.Dispose();
        }
    }

    [Fact] void should_persist_without_waiting_for_the_subscriber() => _marked.ShouldBeTrue();
    [Fact] void should_not_block_the_marking_operation() => _error.ShouldBeNull();
    [Fact] void should_deny_local_delivery_before_notifying_the_subscriber() => _delivered.ShouldBeFalse();
}
