// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.Commands;
using Cratis.Chronicle.Contracts.Occurrences;
using Grpc.Core;
using ProtoBuf.Grpc;

namespace Cratis.Chronicle.Occurrences.for_OccurrenceAttempt.when_retrying_renewal;

public class and_the_remaining_budget_cannot_cover_backoff : given.a_running_attempt
{
    readonly TaskCompletionSource<CommandResult<CompleteOccurrenceResponse>> _reply = new(TaskCreationOptions.RunContinuationsAsynchronously);

    void Establish() => _renew = _ => _reply.Task;

    async Task Because()
    {
        await RenewAtTwentySeconds();
        _clock.Advance(TimeSpan.FromSeconds(39), deliverCallbacks: false);
        _reply.TrySetException(new RpcException(new Status(StatusCode.Unavailable, "temporarily unavailable")));
        await _running.WaitAsync(TimeSpan.FromSeconds(5), TimeProvider.System);
        _clock.Advance(TimeSpan.FromMinutes(2));
    }

    [Fact] void should_not_admit_a_retry_at_the_deadline() => _renewalCount.ShouldEqual(1);
    [Fact] void should_abandon_without_waiting_the_last_second() => _handlerToken.IsCancellationRequested.ShouldBeTrue();
    [Fact] void should_not_complete() => _service.DidNotReceive().CompleteOccurrence(Arg.Any<CompleteOccurrenceRequest>(), Arg.Any<CallContext>());
    [Fact] void should_not_fail() => _service.DidNotReceive().FailOccurrence(Arg.Any<FailOccurrenceRequest>(), Arg.Any<CallContext>());
}
