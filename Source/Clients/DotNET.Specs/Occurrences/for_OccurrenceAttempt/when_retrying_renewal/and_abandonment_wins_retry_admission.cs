// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Occurrences.given;
using Grpc.Core;

namespace Cratis.Chronicle.Occurrences.for_OccurrenceAttempt.when_retrying_renewal;

public class and_abandonment_wins_retry_admission : Specification
{
    readonly ManualOccurrenceClock _clock = new();
    int _invocations;
    bool _backoffAllowed;
    bool _retryAdmitted;
    bool _anotherRetryAllowed;
    Exception _initialError;

    async Task Because()
    {
        using var session = new CancellationTokenSource();
        await using var lifetime = new OccurrenceAttemptLifetime(_clock, _clock.GetTimestamp(), session.Token);
        _clock.Advance(TimeSpan.FromSeconds(20));
        var initial = lifetime.Admit()!;
        _initialError = await Catch.Exception(() => initial.Invoke(_ =>
        {
            _invocations++;
            return Task.FromException<bool>(new RpcException(new Status(StatusCode.Unavailable, "temporarily unavailable")));
        }));
        _backoffAllowed = lifetime.CanRetry();
        _clock.Advance(TimeSpan.FromSeconds(1));

        var ready = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var proceed = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var retry = Task.Run(async () =>
        {
            ready.TrySetResult();
            await proceed.Task;
            var admission = lifetime.Admit();
            _retryAdmitted = admission is not null;
            if (admission is not null) await admission.Invoke(_ => Task.FromResult(++_invocations));
        });
        await ready.Task.WaitAsync(TimeSpan.FromSeconds(5), TimeProvider.System);
        await session.CancelAsync();
        proceed.TrySetResult();
        await retry.WaitAsync(TimeSpan.FromSeconds(5), TimeProvider.System);
        _anotherRetryAllowed = lifetime.CanRetry();
    }

    [Fact] void should_begin_with_a_transport_failure() => _initialError.ShouldBeOfExactType<RpcException>();
    [Fact] void should_allow_backoff_while_the_original_budget_is_active() => _backoffAllowed.ShouldBeTrue();
    [Fact] void should_reject_retry_admission_after_abandonment() => _retryAdmitted.ShouldBeFalse();
    [Fact] void should_not_invoke_the_retry() => _invocations.ShouldEqual(1);
    [Fact] void should_not_allow_another_retry() => _anotherRetryAllowed.ShouldBeFalse();
}
