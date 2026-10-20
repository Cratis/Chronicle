// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.Testing.Events;
using Grpc.Core;

namespace Cratis.Chronicle.Testing.EventSequences.for_SequenceReplies.when_appending_many_once;

public class after_the_reply_is_dropped : Specification
{
    EventStoreForTesting _store;
    EventSourceId _eventSourceId;
    IdempotencyKey _key;
    object[] _events;
    Exception? _error;
    AppendManyResult _retry;
    int _afterDrop;
    int _afterRetry;

    void Establish()
    {
        _store = new EventStoreForTesting();
        _eventSourceId = EventSourceId.New();
        _key = new IdempotencyKey("batch-append");
        _events = [new TestEvent("one"), new TestEvent("two")];
        _store.Replies.DropNext(DroppedReply.Append);
    }

    async Task Because()
    {
        _error = await Catch.Exception(() => _store.EventLog.AppendManyOnce(_eventSourceId, _events, _key));
        _afterDrop = (await _store.EventLog.GetFromSequenceNumber(EventSequenceNumber.First)).Count;
        _retry = await _store.EventLog.AppendManyOnce(_eventSourceId, _events, _key);
        _afterRetry = (await _store.EventLog.GetFromSequenceNumber(EventSequenceNumber.First)).Count;
    }

    void Destroy() => _store.Dispose();

    [Fact] void should_discard_the_committed_reply() => _error.ShouldBeOfExactType<RpcException>();
    [Fact] void should_report_the_reply_as_unavailable() => ((RpcException)_error!).StatusCode.ShouldEqual(StatusCode.Unavailable);
    [Fact] void should_have_committed_the_batch_before_dropping_the_reply() => _afterDrop.ShouldEqual(2);
    [Fact] void should_succeed_on_retry() => _retry.IsSuccess.ShouldBeTrue();
    [Fact] void should_report_both_events_as_already_appended() => _retry.Outcomes.ToArray().ShouldEqual([AppendOutcome.AlreadyAppended, AppendOutcome.AlreadyAppended]);
    [Fact] void should_not_append_the_batch_again() => _afterRetry.ShouldEqual(2);
    [Fact] void should_drop_exactly_one_reply() => _store.Replies.Dropped.ShouldEqual(1);
}
