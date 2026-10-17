// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.Commands;
using Cratis.Chronicle.Contracts.Occurrences;
using Cratis.Chronicle.Contracts.Storage.Occurrences;
using Grpc.Core;

namespace Cratis.Chronicle.Testing.Occurrences.for_DropReplyInterceptor.when_dropping;

public class a_failure_reply : given.an_occurrence
{
    Exception? _error;
    OccurrenceSnapshotResponse _snapshot;

    async Task Because()
    {
        var claim = await Claim();
        _scenario.Replies.DropNext(DroppedReply.FailOccurrence);
        _error = await Catch.Exception(() => _wire.FailOccurrence(new() { EventStore = _claim.EventStore, Namespace = _claim.Namespace, Trigger = _claim.Trigger, OccurrenceId = claim.Id, Epoch = claim.Epoch, Error = "injected failure" }).EnsureSuccess());
        _snapshot = (await _scenario.StatusOf(_recorded.Id))!;
    }

    [Fact] void should_discard_the_failure_reply() => _error.ShouldBeOfExactType<RpcException>();
    [Fact] void should_keep_the_durable_failure() => _snapshot.LastError.ShouldEqual("injected failure");
    [Fact] void should_return_to_pending() => _snapshot.State.ShouldEqual(OccurrenceState.Pending);
    [Fact] void should_keep_backoff() => ((DateTimeOffset)_snapshot.NextAttempt).ShouldEqual(_scenario.Time.Now.AddSeconds(1));
}
