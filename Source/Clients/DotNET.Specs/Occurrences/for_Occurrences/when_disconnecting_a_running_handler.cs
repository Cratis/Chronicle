// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.Commands;
using Cratis.Chronicle.Contracts.Occurrences;
using Cratis.Chronicle.Occurrences.given;
using ProtoBuf.Grpc;

namespace Cratis.Chronicle.Occurrences.for_Occurrences;

public class when_disconnecting_a_running_handler : an_occurrence_client
{
    readonly TaskCompletionSource _started = new(TaskCreationOptions.RunContinuationsAsynchronously);
    CancellationToken _handlerToken;

    void Establish()
    {
        _artifacts.OccurrenceHandlers.Returns([typeof(TestOccurrenceHandler)]);
        _service.ClaimOccurrences(Arg.Any<ClaimOccurrencesRequest>(), Arg.Any<CallContext>()).Returns(CommandResult<ClaimOccurrencesResponse>.Success(Guid.NewGuid(), new() { Claims = [_claim] }));
        _handler.OnHandle = (_, _, token) =>
        {
            _handlerToken = token;
            _started.TrySetResult();
            return new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously).Task.WaitAsync(token);
        };
    }

    async Task Because()
    {
        await _occurrences.Discover();
        await _occurrences.Register();
        await _started.Task.WaitAsync(TimeSpan.FromSeconds(5), TimeProvider.System);
        await _lifecycle.Disconnected().WaitAsync(TimeSpan.FromSeconds(5), TimeProvider.System);
        await _occurrences.DisposeAsync().AsTask().WaitAsync(TimeSpan.FromSeconds(5), TimeProvider.System);
    }

    [Fact] void should_cancel_the_attempt() => _handlerToken.IsCancellationRequested.ShouldBeTrue();
    [Fact] void should_dispose_the_handler() => _handler.Disposed.ShouldBeTrue();
    [Fact] void should_dispose_the_subscription() => _notifications.HasObservers.ShouldBeFalse();
    [Fact] void should_not_fail_the_abandoned_lease() => _service.DidNotReceive().FailOccurrence(Arg.Any<FailOccurrenceRequest>(), Arg.Any<CallContext>());
    [Fact] void should_not_complete_the_abandoned_lease() => _service.DidNotReceive().CompleteOccurrence(Arg.Any<CompleteOccurrenceRequest>(), Arg.Any<CallContext>());
}
