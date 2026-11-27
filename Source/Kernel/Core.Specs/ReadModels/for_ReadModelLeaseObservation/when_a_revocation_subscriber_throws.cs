// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.ReadModels.for_ReadModelLeaseObservation;

public class when_a_revocation_subscriber_throws : for_SinkProvenanceCache.given.a_cached_database
{
    readonly TaskCompletionSource _notified = new(TaskCreationOptions.RunContinuationsAsynchronously);
    Exception? _error;
    bool _marked;

    async Task Because()
    {
        await using var other = _lease.Revoked.Register(() => _notified.TrySetResult());
        await using var throwing = _lease.Revoked.Register(() => throw new InvalidOperationException("subscriber failed"));
        _error = await Catch.Exception(_cached.MarkQuarantined);
        _marked = await _raw.IsQuarantined();
        await _notified.Task.WaitAsync(TimeSpan.FromSeconds(5), TimeProvider.System);
    }

    [Fact] void should_persist_the_restriction() => _marked.ShouldBeTrue();
    [Fact] void should_not_propagate_the_subscriber_failure() => _error.ShouldBeNull();
    [Fact] void should_notify_the_other_subscriber() => _notified.Task.IsCompletedSuccessfully.ShouldBeTrue();
}
