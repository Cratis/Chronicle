// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.Testing.Events;
using Grpc.Core;

namespace Cratis.Chronicle.Testing.EventSequences.for_SequenceReplies.when_appending_once;

public class after_the_reply_is_dropped : Specification
{
    EventStoreForTesting _store;
    EventSourceId _eventSourceId;
    IdempotencyKey _key;
    Exception? _error;
    AppendResult _retry;
    int _afterDrop;
    int _afterRetry;

    void Establish()
    {
        _store = new EventStoreForTesting();
        _eventSourceId = EventSourceId.New();
        _key = new IdempotencyKey("single-append");
        _store.Replies.DropNext(DroppedReply.Append);
    }

    async Task Because()
    {
        _error = await Catch.Exception(() => _store.EventLog.AppendOnce(_eventSourceId, new TestEvent("one"), _key));
        _afterDrop = (await _store.EventLog.GetFromSequenceNumber(EventSequenceNumber.First)).Count;
        _retry = await _store.EventLog.AppendOnce(_eventSourceId, new TestEvent("one"), _key);
        _afterRetry = (await _store.EventLog.GetFromSequenceNumber(EventSequenceNumber.First)).Count;
    }

    void Destroy() => _store.Dispose();

    [Fact] void should_discard_the_committed_reply() => _error.ShouldBeOfExactType<RpcException>();
    [Fact] void should_report_the_reply_as_unavailable() => ((RpcException)_error!).StatusCode.ShouldEqual(StatusCode.Unavailable);
    [Fact] void should_have_committed_the_event_before_dropping_the_reply() => _afterDrop.ShouldEqual(1);
    [Fact] void should_succeed_on_retry() => _retry.IsSuccess.ShouldBeTrue();
    [Fact] void should_report_the_retry_as_already_appended() => _retry.Outcome.ShouldEqual(AppendOutcome.AlreadyAppended);
    [Fact] void should_not_append_the_event_again() => _afterRetry.ShouldEqual(1);
    [Fact] void should_drop_exactly_one_reply() => _store.Replies.Dropped.ShouldEqual(1);
}
