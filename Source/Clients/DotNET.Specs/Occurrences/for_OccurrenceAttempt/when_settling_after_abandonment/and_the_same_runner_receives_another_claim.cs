// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.Occurrences;
using Cratis.Chronicle.Occurrences.given;
using ProtoBuf.Grpc;

namespace Cratis.Chronicle.Occurrences.for_OccurrenceAttempt.when_settling_after_abandonment;

public class and_the_same_runner_receives_another_claim : an_occurrence_client
{
    readonly ManualOccurrenceClock _clock = new();
    readonly TaskCompletionSource _entered = new(TaskCreationOptions.RunContinuationsAsynchronously);
    readonly TaskCompletionSource _release = new(TaskCreationOptions.RunContinuationsAsynchronously);
    int _deliveries;

    void Establish() => _handler.OnHandle = (_, _, _) =>
    {
        if (++_deliveries != 1) return Task.CompletedTask;
        _entered.TrySetResult();
        return _release.Task;
    };

    async Task Because()
    {
        var attempt = CreateAttempt(_clock);
        using var session = new CancellationTokenSource();
        var first = attempt.Run(_claim, _clock.GetTimestamp(), session.Token);
        await _entered.Task.WaitAsync(TimeSpan.FromSeconds(5), TimeProvider.System);
        await session.CancelAsync();
        _release.TrySetResult();
        await first.WaitAsync(TimeSpan.FromSeconds(5), TimeProvider.System);
        _claim.Epoch++;
        _clock.Advance(TimeSpan.FromSeconds(60));
        await attempt.Run(_claim, _clock.GetTimestamp(), CancellationToken.None);
    }

    [Fact] void should_deliver_the_fresh_claim() => _deliveries.ShouldEqual(2);
    [Fact] void should_not_complete_the_abandoned_claim() => _service.DidNotReceive().CompleteOccurrence(Arg.Is<CompleteOccurrenceRequest>(request => request.Epoch == 3), Arg.Any<CallContext>());
    [Fact] void should_complete_the_new_claim() => _service.Received(1).CompleteOccurrence(Arg.Is<CompleteOccurrenceRequest>(request => request.Epoch == 4), Arg.Any<CallContext>());
    [Fact] void should_not_fail() => _service.DidNotReceive().FailOccurrence(Arg.Any<FailOccurrenceRequest>(), Arg.Any<CallContext>());
}
