// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.Commands;
using Cratis.Chronicle.Contracts.Occurrences;
using Cratis.Chronicle.Occurrences.given;
using ProtoBuf.Grpc;

namespace Cratis.Chronicle.Occurrences.for_Occurrences;

public class when_reconnecting_after_a_cancelled_attempt : an_occurrence_client
{
    readonly TaskCompletionSource _started = new(TaskCreationOptions.RunContinuationsAsynchronously);
    readonly TaskCompletionSource _cancelled = new(TaskCreationOptions.RunContinuationsAsynchronously);
    readonly TaskCompletionSource _finish = new(TaskCreationOptions.RunContinuationsAsynchronously);
    readonly TaskCompletionSource _reclaimed = new(TaskCreationOptions.RunContinuationsAsynchronously);
    int _claims;
    bool _previousDisposed;

    void Establish()
    {
        _artifacts.OccurrenceHandlers.Returns([typeof(TestOccurrenceHandler)]);
        _service.ClaimOccurrences(Arg.Any<ClaimOccurrencesRequest>(), Arg.Any<CallContext>()).Returns(_ =>
        {
            var count = Interlocked.Increment(ref _claims);
            if (count == 2)
            {
                _previousDisposed = _handler.Disposed;
                _reclaimed.TrySetResult();
            }
            return CommandResult<ClaimOccurrencesResponse>.Success(Guid.NewGuid(), new() { Claims = count == 1 ? [_claim] : [] });
        });
        _handler.OnHandle = async (_, _, token) =>
        {
            await using var registration = token.Register(() => _cancelled.TrySetResult());
            _started.TrySetResult();

            // Hold the cancelled handler until the test releases it, without an elapsed-time wait.
            await _finish.Task;
        };
    }

    async Task Because()
    {
        await _occurrences.Discover();
        await _occurrences.Register();
        await _started.Task.WaitAsync(TimeSpan.FromSeconds(30), TimeProvider.System);
        await _lifecycle.Disconnected();
        await _cancelled.Task.WaitAsync(TimeSpan.FromSeconds(30), TimeProvider.System);
        await _occurrences.Register().WaitAsync(TimeSpan.FromSeconds(30), TimeProvider.System);
        _finish.SetResult();
        await _reclaimed.Task.WaitAsync(TimeSpan.FromSeconds(30), TimeProvider.System);
        await _occurrences.DisposeAsync();
    }

    void Destroy() => _finish.TrySetResult();

    [Fact] void should_release_the_previous_handler_before_claiming_again() => _previousDisposed.ShouldBeTrue();
    [Fact] void should_not_complete_the_cancelled_attempt() => _service.DidNotReceive().CompleteOccurrence(Arg.Any<CompleteOccurrenceRequest>(), Arg.Any<CallContext>());
    [Fact] void should_release_the_reconnected_subscription() => _notifications.HasObservers.ShouldBeFalse();
}
