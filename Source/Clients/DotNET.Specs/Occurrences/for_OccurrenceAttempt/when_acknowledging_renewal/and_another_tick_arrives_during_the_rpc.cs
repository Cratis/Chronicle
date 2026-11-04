// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.Commands;
using Cratis.Chronicle.Contracts.Occurrences;
using ProtoBuf.Grpc;

namespace Cratis.Chronicle.Occurrences.for_OccurrenceAttempt.when_acknowledging_renewal;

public class and_another_tick_arrives_during_the_rpc : given.a_running_attempt
{
    readonly TaskCompletionSource<CommandResult<CompleteOccurrenceResponse>> _reply = new(TaskCreationOptions.RunContinuationsAsynchronously);

    void Establish() => _renew = _ => _reply.Task;

    async Task Because()
    {
        await RenewAtTwentySeconds();
        _clock.Advance(TimeSpan.FromSeconds(20));
        try { await Finish(); }
        finally { _reply.TrySetResult(Renewed()); }
    }

    [Fact] void should_not_start_an_overlapping_renewal() => _renewalCount.ShouldEqual(1);
    [Fact] void should_cancel_the_outstanding_rpc_on_shutdown() => _renewalToken.IsCancellationRequested.ShouldBeTrue();
    [Fact] void should_complete_the_active_claim_once() => _service.Received(1).CompleteOccurrence(Arg.Any<CompleteOccurrenceRequest>(), Arg.Any<CallContext>());
}
