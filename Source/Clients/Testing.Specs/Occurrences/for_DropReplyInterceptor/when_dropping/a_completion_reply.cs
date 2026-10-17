// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.Commands;
using Cratis.Chronicle.Contracts.Occurrences;
using Cratis.Chronicle.Contracts.Storage.Occurrences;
using Grpc.Core;

namespace Cratis.Chronicle.Testing.Occurrences.for_DropReplyInterceptor.when_dropping;

public class a_completion_reply : given.an_occurrence
{
    Exception? _error;
    OccurrenceLeaseResult _retry;

    async Task Because()
    {
        var claim = await Claim();
        var request = new CompleteOccurrenceRequest { EventStore = _claim.EventStore, Namespace = _claim.Namespace, Trigger = _claim.Trigger, OccurrenceId = claim.Id, Epoch = claim.Epoch };
        _scenario.Replies.DropNext(DroppedReply.CompleteOccurrence);
        _error = await Catch.Exception(() => _wire.CompleteOccurrence(request).EnsureSuccess());
        _retry = (await _wire.CompleteOccurrence(request).EnsureSuccess()).Status;
    }

    [Fact] void should_discard_the_completion_reply() => _error.ShouldBeOfExactType<RpcException>();
    [Fact] void should_settle_the_same_epoch_as_already_completed() => _retry.ShouldEqual(OccurrenceLeaseResult.AlreadyCompleted);
}
