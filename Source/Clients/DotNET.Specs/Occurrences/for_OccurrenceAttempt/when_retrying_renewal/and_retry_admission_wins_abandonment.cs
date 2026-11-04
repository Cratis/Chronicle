// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Occurrences.given;
using Grpc.Core;

namespace Cratis.Chronicle.Occurrences.for_OccurrenceAttempt.when_retrying_renewal;

public class and_retry_admission_wins_abandonment : Specification
{
    readonly ManualOccurrenceClock _clock = new();
    int _invocations;
    bool _backoffAllowed;
    bool _cancelledAtInvocation;
    bool _accepted;
    bool _anotherRetryAllowed;
    bool _anotherAdmission;
    Exception _initialError;
    Exception _reuseError;

    async Task Because()
    {
        using var session = new CancellationTokenSource();
        await using var lifetime = new OccurrenceAttemptLifetime(_clock, _clock.GetTimestamp(), session.Token);
        _clock.Advance(TimeSpan.FromSeconds(20));
        var initial = lifetime.Admit();
        _initialError = await Catch.Exception(() => initial.Invoke(_ =>
        {
            _invocations++;
            return Task.FromException<bool>(new RpcException(new Status(StatusCode.Unavailable, "temporarily unavailable")));
        }));
        _backoffAllowed = lifetime.CanRetry();
        _clock.Advance(TimeSpan.FromSeconds(1));

        var admitted = new TaskCompletionSource<OccurrenceAttemptLifetime.Operation>(TaskCreationOptions.RunContinuationsAsynchronously);
        var proceed = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var retry = Task.Run(async () =>
        {
            var admission = lifetime.Admit();
            admitted.TrySetResult(admission);
            await proceed.Task;
            await admission.Invoke(token =>
            {
                _invocations++;
                _cancelledAtInvocation = token.IsCancellationRequested;
                return Task.FromResult(true);
            });
            _accepted = lifetime.Accept(admission);
        });
        var ticket = await admitted.Task.WaitAsync(TimeSpan.FromSeconds(5), TimeProvider.System);
        await session.CancelAsync();
        proceed.TrySetResult();
        await retry.WaitAsync(TimeSpan.FromSeconds(5), TimeProvider.System);
        _anotherRetryAllowed = lifetime.CanRetry();
        _anotherAdmission = lifetime.Admit() is not null;
        _reuseError = await Catch.Exception(() => ticket.Invoke(_ => Task.FromResult(++_invocations)));
    }

    [Fact] void should_begin_with_a_transport_failure() => _initialError.ShouldBeOfExactType<RpcException>();
    [Fact] void should_allow_backoff_before_abandonment() => _backoffAllowed.ShouldBeTrue();
    [Fact] void should_allow_only_one_previously_admitted_retry_invocation() => _invocations.ShouldEqual(2);
    [Fact] void should_pass_cancellation_to_the_previously_admitted_retry() => _cancelledAtInvocation.ShouldBeTrue();
    [Fact] void should_not_refresh_from_the_late_success() => _accepted.ShouldBeFalse();
    [Fact] void should_not_retry_after_the_late_success() => _anotherRetryAllowed.ShouldBeFalse();
    [Fact] void should_not_issue_another_admission() => _anotherAdmission.ShouldBeFalse();
    [Fact] void should_keep_the_retry_ticket_single_use() => _reuseError.ShouldBeOfExactType<InvalidOperationException>();
}
