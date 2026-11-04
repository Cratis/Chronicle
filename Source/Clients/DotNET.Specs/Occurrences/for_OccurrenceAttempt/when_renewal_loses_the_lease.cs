// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.Commands;
using Cratis.Chronicle.Contracts.Occurrences;
using Cratis.Chronicle.Contracts.Storage.Occurrences;
using Cratis.Chronicle.Occurrences.given;
using ProtoBuf.Grpc;

namespace Cratis.Chronicle.Occurrences.for_OccurrenceAttempt;

public class when_renewal_loses_the_lease : an_occurrence_client
{
    readonly ManualOccurrenceClock _clock = new();

    void Establish()
    {
        _handler.OnHandle = (_, _, token) => new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously).Task.WaitAsync(token);
        _service.RenewOccurrenceLease(Arg.Any<RenewOccurrenceLeaseRequest>(), Arg.Any<CallContext>()).Returns(CommandResult<CompleteOccurrenceResponse>.Success(Guid.NewGuid(), new() { Status = OccurrenceLeaseResult.OccurrenceLeaseLost }));
    }

    async Task Because()
    {
        var running = CreateAttempt(_clock).Run(_claim, _clock.GetTimestamp(), CancellationToken.None);
        var timer = await _clock.WaitForTimer(TimeSpan.FromSeconds(20));
        timer.Tick();
        await running.WaitAsync(TimeSpan.FromSeconds(5), TimeProvider.System);
    }

    [Fact] void should_not_complete_the_claim() => _service.DidNotReceive().CompleteOccurrence(Arg.Any<CompleteOccurrenceRequest>(), Arg.Any<CallContext>());
    [Fact] void should_not_fail_the_claim() => _service.DidNotReceive().FailOccurrence(Arg.Any<FailOccurrenceRequest>(), Arg.Any<CallContext>());
    [Fact] void should_dispose_the_cancelled_handler() => _handler.Disposed.ShouldBeTrue();
}
