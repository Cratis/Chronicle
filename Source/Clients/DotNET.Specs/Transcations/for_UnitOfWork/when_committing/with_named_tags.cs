// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Auditing;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.EventSequences.Concurrency;

namespace Cratis.Chronicle.Transactions.for_UnitOfWork.when_committing;

public class with_named_tags : given.a_unit_of_work
{
    EventForEventSourceId[] _taggedEvents;
    EventSourceId _sourceId;

    void Establish()
    {
        _sourceId = EventSourceId.New();
        _eventSequence.AppendMany(
            Arg.Any<IEnumerable<EventForEventSourceId>>(),
            Arg.Any<IEnumerable<NamedTag>>(),
            Arg.Any<CorrelationId?>(),
            Arg.Any<IEnumerable<string>>(),
            Arg.Any<IDictionary<EventSourceId, ConcurrencyScope>>())
            .Returns(call =>
            {
                _taggedEvents = call.Arg<IEnumerable<EventForEventSourceId>>().ToArray();
                return _appendResult;
            });
        _unitOfWork.AddEvent(EventSequenceId.Log, _sourceId, "one", [new("event", "one")], Causation.Unknown());
        _unitOfWork.AddEvents(EventSequenceId.Log,
            [new EventForEventSourceId(_sourceId, "two") { NamedTags = [new("event", "two"), new("shared", "same"), new("shared", "same"), new("call", "value")] }],
            []);
    }

    async Task Because() => await _unitOfWork.Commit();

    [Fact] void should_send_both_events_on_tagged_path() => _taggedEvents.Select(_ => _.Event).ShouldEqual(["one", "two"]);
    [Fact] void should_keep_single_event_named_tags() => _taggedEvents[0].NamedTags.Select(_ => _.Value).ShouldEqual(["one"]);
    [Fact] void should_merge_batch_tags_event_first() => _taggedEvents[^1].NamedTags.Select(_ => (_.Name.Value, _.Value)).ShouldEqual([("event", "two"), ("shared", "same"), ("call", "value")]);
    [Fact] void should_not_use_legacy_overload() => _eventSequence.DidNotReceive().AppendMany(Arg.Any<IEnumerable<EventForEventSourceId>>(), Arg.Any<CorrelationId?>(), Arg.Any<IEnumerable<string>>(), Arg.Any<IDictionary<EventSourceId, ConcurrencyScope>>());
}
