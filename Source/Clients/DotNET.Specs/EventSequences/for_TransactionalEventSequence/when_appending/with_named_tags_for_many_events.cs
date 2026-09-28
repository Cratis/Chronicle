// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Auditing;
using Cratis.Chronicle.Events;

namespace Cratis.Chronicle.EventSequences.for_TransactionalEventSequence.when_appending;

public class with_named_tags_for_many_events : given.a_transactional_event_sequence
{
    EventSourceId _sourceId;
    List<IEnumerable<NamedTag>> _received;
    int _enumerations;

    void Establish()
    {
        _sourceId = EventSourceId.New();
        _received = [];
        _unitOfWork.When(_ => _.AddEventWithNamedTags(_eventSequence.Id, _sourceId, Arg.Any<object>(), Arg.Any<IEnumerable<NamedTag>>(), Arg.Any<Causation>()))
            .Do(call => _received.Add(call.Arg<IEnumerable<NamedTag>>()));
    }

    Task Because() => _transactionalEventSequence.AppendManyWithNamedTags(_sourceId, ["one", "two"], YieldOnce());

    [Fact] void should_enroll_each_event_with_the_same_named_tag() => _received.Select(_ => _.Single().Value).ShouldEqual(["value", "value"]);
    [Fact] void should_materialize_the_call_tags_only_once() => _enumerations.ShouldEqual(1);

    IEnumerable<NamedTag> YieldOnce()
    {
        _enumerations++;
        if (_enumerations != 1)
        {
            throw new Exception("Named tags were enumerated more than once");
        }
        yield return new NamedTag("name", "value");
    }
}
