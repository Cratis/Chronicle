// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.Commands;
using Cratis.Chronicle.Contracts.Occurrences;
using Cratis.Chronicle.Contracts.Queries;
using Cratis.Chronicle.Occurrences.given;
using ProtoBuf.Grpc;

namespace Cratis.Chronicle.Occurrences.for_Occurrences;

public class when_delivering_on_a_notification : an_occurrence_client
{
    readonly TaskCompletionSource _initialClaim = new(TaskCreationOptions.RunContinuationsAsynchronously);
    readonly TaskCompletionSource _completed = new(TaskCreationOptions.RunContinuationsAsynchronously);
    int _claims;
    bool _pending;

    void Establish()
    {
        _artifacts.OccurrenceHandlers.Returns([typeof(TestOccurrenceHandler)]);
        _service.ClaimOccurrences(Arg.Any<ClaimOccurrencesRequest>(), Arg.Any<CallContext>()).Returns(_ =>
        {
            Interlocked.Increment(ref _claims);
            _initialClaim.TrySetResult();
            var batch = _pending ? new[] { _claim } : [];
            _pending = false;
            return CommandResult<ClaimOccurrencesResponse>.Success(Guid.NewGuid(), new() { Claims = batch });
        });
        _service.CompleteOccurrence(Arg.Any<CompleteOccurrenceRequest>(), Arg.Any<CallContext>()).Returns(_ =>
        {
            _completed.TrySetResult();
            return CommandResult<CompleteOccurrenceResponse>.Success(Guid.NewGuid(), new());
        });
    }

    async Task Because()
    {
        await _occurrences.Discover();
        await _occurrences.Register();
        await _initialClaim.Task.WaitAsync(TimeSpan.FromSeconds(5), TimeProvider.System);
        _pending = true;
        _notifications.OnNext(QueryResult<IEnumerable<OccurrenceSnapshotResponse>>.Success(Guid.NewGuid(), []));
        await _completed.Task.WaitAsync(TimeSpan.FromSeconds(5), TimeProvider.System);
        await _occurrences.DisposeAsync();
    }

    [Fact] void should_wake_and_claim_again() => (_claims >= 2).ShouldBeTrue();
    [Fact] void should_use_the_connection_identity_for_expiry() => _service.Received().ClaimOccurrences(Arg.Is<ClaimOccurrencesRequest>(request => request.WorkerId == _lifecycle.ConnectionId.ToString() && request.Max == 1), Arg.Any<CallContext>());
    [Fact] void should_release_the_observation_subscription() => _notifications.HasObservers.ShouldBeFalse();
}
