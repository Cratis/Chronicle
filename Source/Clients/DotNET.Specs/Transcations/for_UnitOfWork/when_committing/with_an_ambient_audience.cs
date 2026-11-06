// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Auditing;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.EventSequences.Concurrency;

namespace Cratis.Chronicle.Transactions.for_UnitOfWork.when_committing;

public class with_an_ambient_audience : given.a_unit_of_work
{
    EventForEventSourceId[] _taggedEvents;

    void Establish()
    {
        _eventSequence.AppendManyWithNamedTags(
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

        using (EventTags.Audience("lobby-a"))
        {
            _unitOfWork.AddEvent(EventSequenceId.Log, EventSourceId.New(), new RequiresAudience(), Causation.Unknown());
        }
    }

    async Task Because() => await _unitOfWork.Commit();

    [Fact] void should_keep_the_audience_stamped_at_enrollment_after_the_scope_closed() => _taggedEvents.Single().NamedTags.Single().ShouldEqual(new NamedTag(new TagName(WellKnownTags.Audience), "lobby-a"));

    [Audience]
    record RequiresAudience;
}
