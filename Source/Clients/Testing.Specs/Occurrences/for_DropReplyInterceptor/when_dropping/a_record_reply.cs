// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Occurrences;
using Grpc.Core;

namespace Cratis.Chronicle.Testing.Occurrences.for_DropReplyInterceptor.when_dropping;

public class a_record_reply : given.an_occurrence
{
    Exception? _error;
    OccurrenceRecordResult _retry;

    async Task Because()
    {
        _scenario.Replies.DropNext(DroppedReply.RecordOccurrence);
        _error = await Catch.Exception(() => _scenario.Record(new ImportRequested("two"), "request-two"));
        _retry = await _scenario.Record(new ImportRequested("two"), "request-two");
    }

    [Fact] void should_drop_after_commit() => _error.ShouldBeOfExactType<RpcException>();
    [Fact] void should_simulate_unavailability() => ((RpcException)_error!).StatusCode.ShouldEqual(StatusCode.Unavailable);
    [Fact] void should_find_the_original_occurrence_on_retry() => _retry.Status.ShouldEqual(OccurrenceRecordStatus.AlreadyRecorded);
    [Fact] void should_discard_exactly_one_reply() => _scenario.Replies.Dropped.ShouldEqual(1);
}
