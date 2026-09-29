// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using System.Reactive.Subjects;
using Cratis.Chronicle.Changes;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.EventTypes;
using Cratis.Chronicle.Dynamic;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.Properties;
using Cratis.Chronicle.Schemas;
using Cratis.Chronicle.Storage.EventSequences;
using Cratis.Chronicle.Storage.EventTypes;
using Cratis.Monads;
using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle.Projections.Engine.for_ProjectionEventContextExtensions.given;

/// <summary>
/// A join resolving a stored event, where the generation the event was stored at is not the one the projection was built with.
/// </summary>
public class a_join_of_a_stored_event_with_pii : Specification
{
    protected const string AdvisorId = "advisor-1";

    protected static readonly EventType first_generation = new(new EventTypeId("advisor-named"), EventTypeGeneration.First);
    protected static readonly EventType second_generation = new(new EventTypeId("advisor-named"), new EventTypeGeneration(2));

    protected IEventSequenceStorage _eventSequenceStorage;
    protected IEventCompliance _eventCompliance;
    protected IEventTypesStorage _eventTypes;
    protected AppendedEvent _storedEvent;
    protected AppendedEvent _releasedEvent;
    protected JsonSchema _projectionSchema;
    protected JsonSchema _storedSchema;
    protected List<ProjectionEventContext> _results = [];

    Subject<ProjectionEventContext> _subject;
    IChangeset<AppendedEvent, ExpandoObject> _changeset;
    int _sequenceNumber = 2;

    void Establish()
    {
        _eventSequenceStorage = Substitute.For<IEventSequenceStorage>();
        _eventCompliance = Substitute.For<IEventCompliance>();
        _eventTypes = Substitute.For<IEventTypesStorage>();
        _projectionSchema = SchemaWithPii();
        _storedSchema = SchemaWithPii();
    }

    /// <summary>
    /// Stores the join event at the given generation and starts resolving a join for a projection built with another one.
    /// </summary>
    /// <param name="storedAt">The <see cref="EventType"/> the join event is stored at.</param>
    /// <param name="projectionBuiltWith">The <see cref="EventType"/> the projection - and its schema - was built with.</param>
    /// <param name="storedSchemaKnown">Whether or not the event types storage knows the schema of the stored generation.</param>
    protected void ResolveJoinOfEventStoredAt(EventType storedAt, EventType projectionBuiltWith, bool storedSchemaKnown = true)
    {
        _storedEvent = new AppendedEvent(
            EventContext.EmptyWithEventSourceId(AdvisorId) with
            {
                EventType = storedAt,
                SequenceNumber = EventSequenceNumber.First,
                Subject = (Subject)AdvisorId
            },
            new ExpandoObject());
        _releasedEvent = _storedEvent with { Content = new { name = "released" }.AsExpandoObject() };

        _eventSequenceStorage
            .TryGetLastEventBefore(Arg.Any<EventTypeId>(), AdvisorId, Arg.Any<EventSequenceNumber>())
            .Returns(Task.FromResult(Catch<Option<AppendedEvent>>.Success(new Option<AppendedEvent>(_storedEvent))));
        _eventCompliance.Release(Arg.Any<AppendedEvent>(), Arg.Any<JsonSchema>()).Returns(_releasedEvent);
        _eventTypes.GetFor(Arg.Any<IEnumerable<EventType>>())
            .Returns(Task.FromResult<IEnumerable<EventTypeSchema>>(
                storedSchemaKnown ? [new EventTypeSchema(storedAt, EventTypeOwner.Client, EventTypeSource.Code, _storedSchema)] : []));

        dynamic state = new ExpandoObject();
        state.advisorId = AdvisorId;
        _changeset = Substitute.For<IChangeset<AppendedEvent, ExpandoObject>>();
        _changeset.CurrentState.Returns((ExpandoObject)state);
        _changeset.ResolvedJoin(Arg.Any<PropertyPath>(), Arg.Any<object>(), Arg.Any<AppendedEvent>(), Arg.Any<ArrayIndexers>())
            .Returns(_changeset);

        _subject = new Subject<ProjectionEventContext>();
        _subject
            .ResolveJoin(
                _eventSequenceStorage,
                projectionBuiltWith,
                new PropertyPath("advisorId"),
                Substitute.For<ILogger>(),
                _eventCompliance,
                _projectionSchema,
                _eventTypes)
            .Subscribe(_results.Add);

        PublishEventThatNeedsTheJoin();
    }

    protected void PublishEventThatNeedsTheJoin()
    {
        var currentEvent = new AppendedEvent(
            EventContext.EmptyWithEventSourceId("case-1") with { SequenceNumber = (ulong)_sequenceNumber++ },
            new ExpandoObject());
        _subject.OnNext(new(
            new(currentEvent.Context.EventSourceId, ArrayIndexers.NoIndexers),
            currentEvent,
            _changeset,
            ProjectionOperationType.From,
            false));
    }

    protected static JsonSchema SchemaWithPii()
    {
        var schema = new JsonSchema();
        schema.Properties["name"] = new JsonSchemaProperty
        {
            ExtensionData = new Dictionary<string, object?>
            {
                { ComplianceJsonSchemaExtensions.ComplianceKey, new[] { new ComplianceSchemaMetadata("PII", string.Empty) } }
            }
        };
        return schema;
    }
}
