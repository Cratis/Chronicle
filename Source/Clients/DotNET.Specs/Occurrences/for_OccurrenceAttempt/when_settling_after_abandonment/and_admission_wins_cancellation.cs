// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Occurrences.given;

namespace Cratis.Chronicle.Occurrences.for_OccurrenceAttempt.when_settling_after_abandonment;

public class and_admission_wins_cancellation : Specification
{
    int _invocations;
    bool _cancelledAtInvocation;
    bool _newAdmission;
    Exception _reuseError;

    async Task Because()
    {
        var clock = new ManualOccurrenceClock();
        using var session = new CancellationTokenSource();
        await using var lifetime = new OccurrenceAttemptLifetime(clock, clock.GetTimestamp(), session.Token);
        var admitted = new TaskCompletionSource<OccurrenceAttemptLifetime.Operation>(TaskCreationOptions.RunContinuationsAsynchronously);
        var proceed = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var settlement = Task.Run(async () =>
        {
            var ticket = lifetime.Admit(settlement: true);
            admitted.TrySetResult(ticket);
            await proceed.Task;
            await ticket.Invoke(token =>
            {
                _invocations++;
                _cancelledAtInvocation = token.IsCancellationRequested;
                return Task.FromResult(true);
            });
        });
        var ticket = await admitted.Task.WaitAsync(TimeSpan.FromSeconds(5), TimeProvider.System);
        await session.CancelAsync();
        proceed.TrySetResult();
        await settlement.WaitAsync(TimeSpan.FromSeconds(5), TimeProvider.System);
        _newAdmission = lifetime.Admit(settlement: true) is not null;
        _reuseError = await Catch.Exception(() => ticket.Invoke(_ => Task.FromResult(++_invocations)));
    }

    [Fact] void should_allow_the_previously_admitted_invocation() => _invocations.ShouldEqual(1);
    [Fact] void should_pass_the_cancelled_lifetime() => _cancelledAtInvocation.ShouldBeTrue();
    [Fact] void should_not_allow_a_new_settlement() => _newAdmission.ShouldBeFalse();
    [Fact] void should_make_the_ticket_single_use() => _reuseError.ShouldBeOfExactType<InvalidOperationException>();
}
