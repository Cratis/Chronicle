// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.Commands;
using Cratis.Chronicle.Contracts.Occurrences;
using ProtoBuf.Grpc;

namespace Cratis.Chronicle.Occurrences.for_OccurrenceAttempt.when_retrying_renewal;

public class and_the_command_is_rejected : given.a_running_attempt
{
    void Establish() => _renew = _ => Task.FromResult(new CommandResult<CompleteOccurrenceResponse> { IsAuthorized = false });

    async Task Because()
    {
        await RenewAtTwentySeconds();
        await _running.WaitAsync(TimeSpan.FromSeconds(5), TimeProvider.System);
        _clock.Advance(TimeSpan.FromMinutes(2));
    }

    [Fact] void should_not_retry() => _renewalCount.ShouldEqual(1);
    [Fact] void should_cancel_the_handler() => _handlerToken.IsCancellationRequested.ShouldBeTrue();
    [Fact] void should_not_complete() => _service.DidNotReceive().CompleteOccurrence(Arg.Any<CompleteOccurrenceRequest>(), Arg.Any<CallContext>());
    [Fact] void should_not_fail() => _service.DidNotReceive().FailOccurrence(Arg.Any<FailOccurrenceRequest>(), Arg.Any<CallContext>());
}
