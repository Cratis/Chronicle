// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.Namespaces.for_NamespaceCommitRetries;

public class when_backoff_is_cancelled : given.a_retry_budget
{
    TaskCompletionSource _entered;
    TaskCompletionSource _pending;
    NamespaceCommitCancellation _lifetime;

    void Establish()
    {
        _entered = new(TaskCreationOptions.RunContinuationsAsynchronously);
        _pending = new(TaskCreationOptions.RunContinuationsAsynchronously);
        _delay = (_, token) =>
        {
            _entered.SetResult();
            return _pending.Task.WaitAsync(token);
        };
    }

    async Task Because()
    {
        _lifetime = _retries.Bind(_context);
        await _retries.BeginAttempt(_lifetime);
        var retry = _retries.BeginAttempt(_lifetime);
        await _entered.Task;
        await _activation.CancelAsync();
        _error = await Catch.Exception(() => retry);
    }

    [Fact] void should_cancel_the_pending_backoff() => _error.ShouldBeAssignableFrom<OperationCanceledException>();
    [Fact] void should_not_consume_an_attempt_after_cancellation() => _retries.HasRemainingAttempts.ShouldBeTrue();

    void Destroy() => _lifetime?.Dispose();
}
