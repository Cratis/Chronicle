// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reactive.Subjects;
using Cratis.Chronicle.Contracts.Commands;
using Cratis.Chronicle.Contracts.Occurrences;
using Cratis.Chronicle.Contracts.Queries;
using Cratis.Chronicle.Occurrences.given;
using ProtoBuf.Grpc;

namespace Cratis.Chronicle.Occurrences.for_Occurrences;

public class when_the_observation_stream_breaks : an_occurrence_client
{
    readonly ManualOccurrenceClock _clock = new();
    readonly TaskCompletionSource _attached = new(TaskCreationOptions.RunContinuationsAsynchronously);
    readonly TaskCompletionSource _reattached = new(TaskCreationOptions.RunContinuationsAsynchronously);
    readonly Subject<QueryResult<IEnumerable<OccurrenceSnapshotResponse>>> _reconnected = new();
    int _claims;
    int _subscriptions;

    void Establish()
    {
        _occurrences.TimeProvider = _clock;
        _artifacts.OccurrenceHandlers.Returns([typeof(TestOccurrenceHandler)]);
        _service.ObserveOccurrences(Arg.Any<ObserveOccurrencesRequest>(), Arg.Any<CallContext>()).Returns(_ => Interlocked.Increment(ref _subscriptions) == 1 ? _notifications : _reconnected);
        _service.ClaimOccurrences(Arg.Any<ClaimOccurrencesRequest>(), Arg.Any<CallContext>()).Returns(_ =>
        {
            if (Interlocked.Increment(ref _claims) == 1) _attached.TrySetResult();
            else _reattached.TrySetResult();
            return CommandResult<ClaimOccurrencesResponse>.Success(Guid.NewGuid(), new());
        });
    }

    async Task Because()
    {
        await _occurrences.Discover();
        await _occurrences.Register();
        await _attached.Task.WaitAsync(TimeSpan.FromSeconds(5), TimeProvider.System);
        _notifications.OnCompleted();
        var reconnect = await _clock.Created.Task.WaitAsync(TimeSpan.FromSeconds(5), TimeProvider.System);
        reconnect.Tick();
        await _reattached.Task.WaitAsync(TimeSpan.FromSeconds(5), TimeProvider.System);
        await _occurrences.DisposeAsync();
    }

    void Destroy() => _reconnected.Dispose();

    [Fact] void should_reattach_the_status_stream() => _service.Received(2).ObserveOccurrences(Arg.Any<ObserveOccurrencesRequest>(), Arg.Any<CallContext>());
    [Fact] void should_claim_from_durable_state_without_a_new_notification() => _claims.ShouldEqual(2);
}
