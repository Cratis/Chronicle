// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using Cratis.Chronicle.Changes;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Properties;
using Cratis.Chronicle.Storage.EventSequences;
using Cratis.Chronicle.Storage.Sinks;
using Microsoft.Extensions.Logging;
using NJsonSchema;

namespace Cratis.Chronicle.Projections.Engine.Pipelines.Steps.for_HandleEvent.given;

public class a_handle_event_step : Specification
{
    protected HandleEvent _step;
    protected IEventSequenceStorage _eventSequenceStorage;
    protected ISink _sink;
    protected ILogger<HandleEvent> _logger;
    protected IProjection _projection;
    protected ProjectionEventContext _context;
    protected IChangeset<AppendedEvent, ExpandoObject> _changeset;
    protected AppendedEvent _event;
    protected Key _key;
    protected ExpandoObject _projectionInitialModelState;

    void Establish()
    {
        _eventSequenceStorage = Substitute.For<IEventSequenceStorage>();
        _sink = Substitute.For<ISink>();
        _logger = Substitute.For<ILogger<HandleEvent>>();
        _step = new HandleEvent(_eventSequenceStorage, _sink, _logger);

        _key = new Key("test-key", ArrayIndexers.NoIndexers);
        _changeset = Substitute.For<IChangeset<AppendedEvent, ExpandoObject>>();
        _changeset.Changes.Returns(new List<Change>());
        _changeset.CurrentState.Returns(new ExpandoObject());
        _projectionInitialModelState = new ExpandoObject();

        var eventType = new EventType("TestEvent", EventTypeGeneration.First);
        _event = AppendedEvent.EmptyWithEventType(eventType);

        _context = new ProjectionEventContext(
            _key,
            _event,
            _changeset,
            ProjectionOperationType.None,
            NeedsInitialState: false);

        _projection = Substitute.For<IProjection>();
        _projection.InitialModelState.Returns(_projectionInitialModelState);
        _projection.TargetReadModelSchema.Returns(new JsonSchema { Title = "TestModel" });
    }
}
