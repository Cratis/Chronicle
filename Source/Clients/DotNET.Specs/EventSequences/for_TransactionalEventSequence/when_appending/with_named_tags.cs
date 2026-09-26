// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Auditing;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSequences.Concurrency;

namespace Cratis.Chronicle.EventSequences.for_TransactionalEventSequence.when_appending;

public class with_named_tags : given.a_transactional_event_sequence
{
    EventSourceId _sourceId;
    NamedTag[] _namedTags;

    void Establish()
    {
        _sourceId = EventSourceId.New();
        _namedTags = [new("name", "value")];
    }

    Task Because() => _transactionalEventSequence.AppendWithNamedTags(_sourceId, "event", _namedTags);

    [Fact] void should_enroll_in_the_current_unit_of_work() => _unitOfWork.Received(1).AddEventWithNamedTags(EventSequenceId.Log, _sourceId, "event", _namedTags, Arg.Any<Causation>(), Arg.Any<EventStreamType?>(), Arg.Any<EventStreamId?>(), Arg.Any<EventSourceType?>(), Arg.Any<ConcurrencyScope?>(), Arg.Any<IEnumerable<string>>(), Arg.Any<DateTimeOffset?>(), Arg.Any<Subject?>());
    [Fact] void should_not_call_the_legacy_enrollment() => _unitOfWork.DidNotReceive().AddEvent(EventSequenceId.Log, _sourceId, "event", Arg.Any<Causation>(), Arg.Any<EventStreamType?>(), Arg.Any<EventStreamId?>(), Arg.Any<EventSourceType?>(), Arg.Any<ConcurrencyScope?>(), Arg.Any<IEnumerable<string>>(), Arg.Any<DateTimeOffset?>(), Arg.Any<Subject?>());
}
