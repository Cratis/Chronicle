// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using System.Reactive.Subjects;
using Cratis.Chronicle.Changes;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Dynamic;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.Properties;
using Cratis.Chronicle.Schemas;
using Cratis.Chronicle.Storage.EventSequences;
using Cratis.Monads;
using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle.Projections.Engine.for_ProjectionEventContextExtensions.when_resolving_join;

public class and_the_stored_event_contains_pii : Specification
{
    const string AdvisorId = "advisor-1";

    Subject<ProjectionEventContext> _subject;
    IEventSequenceStorage _eventSequenceStorage;
    IEventCompliance _eventCompliance;
    AppendedEvent _storedEvent;
    AppendedEvent _releasedEvent;
    JsonSchema _schema;
    ProjectionEventContext _result;

    void Establish()
    {
        _subject = new Subject<ProjectionEventContext>();
        _eventSequenceStorage = Substitute.For<IEventSequenceStorage>();
        _eventCompliance = Substitute.For<IEventCompliance>();
        _schema = new JsonSchema();
        _schema.Properties["name"] = new JsonSchemaProperty
        {
            ExtensionData = new Dictionary<string, object?>
            {
                { ComplianceJsonSchemaExtensions.ComplianceKey, new[] { new ComplianceSchemaMetadata("PII", string.Empty) } }
            }
        };

        _storedEvent = new AppendedEvent(
            EventContext.EmptyWithEventSourceId(AdvisorId) with
            {
                SequenceNumber = EventSequenceNumber.First,
                Subject = (Cratis.Chronicle.Concepts.Events.Subject)AdvisorId
            },
            new ExpandoObject());
        _releasedEvent = _storedEvent with { Content = new { name = "released" }.AsExpandoObject() };

        _eventSequenceStorage
            .TryGetLastEventBefore(Arg.Any<EventTypeId>(), AdvisorId, Arg.Any<EventSequenceNumber>())
            .Returns(Task.FromResult(Catch<Option<AppendedEvent>>.Success(new Option<AppendedEvent>(_storedEvent))));
        _eventCompliance.Release(_storedEvent, _schema).Returns(_releasedEvent);

        var currentEvent = new AppendedEvent(
            EventContext.EmptyWithEventSourceId("case-1") with { SequenceNumber = 2 },
            new ExpandoObject());
        dynamic state = new ExpandoObject();
        state.advisorId = AdvisorId;
        var changeset = Substitute.For<IChangeset<AppendedEvent, ExpandoObject>>();
        changeset.CurrentState.Returns((ExpandoObject)state);
        changeset.ResolvedJoin(Arg.Any<PropertyPath>(), Arg.Any<object>(), Arg.Any<AppendedEvent>(), Arg.Any<ArrayIndexers>())
            .Returns(changeset);

        _subject
            .ResolveJoin(
                _eventSequenceStorage,
                new EventType(new EventTypeId("advisor-named"), 1),
                new PropertyPath("advisorId"),
                Substitute.For<ILogger>(),
                _eventCompliance,
                _schema)
            .Subscribe(_ => _result = _);

        _subject.OnNext(new(
            new(currentEvent.Context.EventSourceId, ArrayIndexers.NoIndexers),
            currentEvent,
            changeset,
            ProjectionOperationType.From,
            false));
    }

    [Fact] void should_release_the_stored_event_before_projecting_it() => _eventCompliance.Received(1).Release(_storedEvent, _schema);
    [Fact] void should_use_the_released_event_as_the_resolved_join_source() => _result.Event.ShouldEqual(_releasedEvent);
}
