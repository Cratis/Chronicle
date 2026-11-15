// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.Occurrences;
using ProtoBuf.Grpc;

namespace Cratis.Chronicle.Occurrences.for_OccurrenceAttempt.when_settling_after_abandonment;

public class and_the_late_handler_faults : given.a_running_attempt
{
    void Establish() => _handler.OnHandle = (_, _, token) =>
    {
        _handlerToken = token;
        _entered.TrySetResult();
        return _release.Task;
    };

    async Task Because()
    {
        await Start();
        await _session.CancelAsync();
        _release.TrySetException(new LateHandlerFailure());
        await _running.WaitAsync(TimeSpan.FromSeconds(5), TimeProvider.System);
    }

    [Fact] void should_dispose_the_late_handler() => _handler.Disposed.ShouldBeTrue();
    [Fact] void should_not_complete() => _service.DidNotReceive().CompleteOccurrence(Arg.Any<CompleteOccurrenceRequest>(), Arg.Any<CallContext>());
    [Fact] void should_not_fail_the_abandoned_claim() => _service.DidNotReceive().FailOccurrence(Arg.Any<FailOccurrenceRequest>(), Arg.Any<CallContext>());

    sealed class LateHandlerFailure : Exception;
}
