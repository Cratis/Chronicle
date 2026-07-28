// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using Cratis.Chronicle.Changes;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Concepts.Projections;
using Cratis.Chronicle.Properties;
using Cratis.Chronicle.Schemas;
using Cratis.Chronicle.Storage.EventSequences;
using Cratis.Chronicle.Storage.Sinks;
using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle.Projections.Engine.Pipelines.Steps.for_key_resolution_memoization.given;

public class a_resolve_key_and_handle_event_step : Specification
{
    protected ResolveKey _resolveKey;
    protected HandleEvent _handleEvent;
    protected IEventSequenceStorage _eventSequenceStorage;
    protected ISink _sink;
    protected ITypeFormats _typeFormats;
    protected IProjection _projection;
    protected IProjection _child;
    protected ProjectionEventContext _context;
    protected AppendedEvent _event;
    protected EventType _eventType;

    protected int _sharedResolverCallCount;
    protected KeyResolver _sharedResolver;

    void Establish()
    {
        _eventSequenceStorage = Substitute.For<IEventSequenceStorage>();
        _sink = Substitute.For<ISink>();
        _typeFormats = Substitute.For<ITypeFormats>();
        _resolveKey = new ResolveKey(_eventSequenceStorage, _sink, _typeFormats, Substitute.For<ILogger<ResolveKey>>());
        _handleEvent = new HandleEvent(_eventSequenceStorage, _sink, Substitute.For<ILogger<HandleEvent>>());

        _eventType = new EventType("TestEvent", EventTypeGeneration.First);
        _event = AppendedEvent.EmptyWithEventType(_eventType);

        var resolvedKey = new Key("resolved-key", ArrayIndexers.NoIndexers);
        _sharedResolver = (_, _, _) =>
        {
            _sharedResolverCallCount++;
            return Task.FromResult(KeyResolverResult.Resolved(resolvedKey));
        };

        var changeset = Substitute.For<IChangeset<AppendedEvent, ExpandoObject>>();
        changeset.Changes.Returns(new List<Change>());
        changeset.CurrentState.Returns(new ExpandoObject());

        _context = new ProjectionEventContext(
            Key.Undefined,
            _event,
            changeset,
            ProjectionOperationType.None,
            NeedsInitialState: false);

        _child = Substitute.For<IProjection>();
        _child.Path.Returns(new ProjectionPath("Items"));
        _child.Identifier.Returns(new ProjectionId("items-child"));
        _child.ChildrenPropertyPath.Returns(new PropertyPath("Items"));
        _child.ChildProjections.Returns([]);
        _child.Accepts(_eventType).Returns(false);
        _child.GetOperationTypeFor(_eventType).Returns(ProjectionOperationType.None);

        _projection = Substitute.For<IProjection>();
        _projection.Identifier.Returns(new ProjectionId("root-projection"));
        _projection.Path.Returns(new ProjectionPath("Root"));
        _projection.ChildrenPropertyPath.Returns(PropertyPath.Root);
        _projection.InitialModelState.Returns(new ExpandoObject());
        _projection.TargetReadModelSchema.Returns(new JsonSchema { Title = "TestModel" });
        _projection.Accepts(_eventType).Returns(true);
        _projection.ChildProjections.Returns([_child]);
    }
}
