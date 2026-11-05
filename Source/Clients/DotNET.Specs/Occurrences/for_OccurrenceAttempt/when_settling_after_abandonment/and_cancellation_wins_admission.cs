// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Occurrences.given;

namespace Cratis.Chronicle.Occurrences.for_OccurrenceAttempt.when_settling_after_abandonment;

public class and_cancellation_wins_admission : Specification
{
    bool _admitted;
    bool _invoked;

    async Task Because()
    {
        var clock = new ManualOccurrenceClock();
        using var session = new CancellationTokenSource();
        await using var lifetime = new OccurrenceAttemptLifetime(clock, clock.GetTimestamp(), session.Token);
        var ready = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var proceed = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var settlement = Task.Run(async () =>
        {
            ready.TrySetResult();
            await proceed.Task;
            var ticket = lifetime.Admit(settlement: true);
            _admitted = ticket is not null;
            if (ticket is not null)
            {
                await ticket.Invoke(_ =>
                {
                    _invoked = true;
                    return Task.FromResult(true);
                });
            }
        });
        await ready.Task.WaitAsync(TimeSpan.FromSeconds(5), TimeProvider.System);
        await session.CancelAsync();
        proceed.TrySetResult();
        await settlement.WaitAsync(TimeSpan.FromSeconds(5), TimeProvider.System);
    }

    [Fact] void should_refuse_a_new_settlement_ticket() => _admitted.ShouldBeFalse();
    [Fact] void should_not_invoke_the_service() => _invoked.ShouldBeFalse();
}
