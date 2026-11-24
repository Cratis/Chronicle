// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.Namespaces.for_NamespaceCommitRetries;

public class when_the_deadline_fires_during_backoff : given.a_retry_budget
{
    TaskCompletionSource _pending;
    NamespaceCommitCancellation _lifetime;

    void Establish()
    {
        _pending = new(TaskCreationOptions.RunContinuationsAsynchronously);
        _context = _context with { Deadline = _clock.GetUtcNow() + TimeSpan.FromSeconds(1) };
        _delay = (_, token) => _pending.Task.WaitAsync(token);
    }

    async Task Because()
    {
        _lifetime = _retries.Bind(_context);
        await _retries.BeginAttempt(_lifetime);
        var retry = _retries.BeginAttempt(_lifetime);
        _clock.Advance(TimeSpan.FromSeconds(1));
        _error = await Catch.Exception(() => retry);
    }

    [Fact] void should_cancel_the_pending_delay_without_sleeping() => _error.ShouldBeAssignableFrom<OperationCanceledException>();
    [Fact] void should_not_consume_another_attempt() => _retries.HasRemainingAttempts.ShouldBeTrue();

    void Destroy() => _lifetime?.Dispose();
}
