// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using Cratis.Chronicle.Changes;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Concepts.Projections;
using Cratis.Chronicle.Properties;
using Cratis.Chronicle.Schemas;
using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle.Projections.Engine.Pipelines.Steps.for_ResolveFutures.given;

public class a_resolve_futures_step : Specification
{
    protected ResolveFutures _step;
    protected IProjectionFutures _projectionFutures;
    protected ProjectionFuturesTracker _tracker;
    protected ITypeFormats _typeFormats;
    protected IObjectComparer _objectComparer;
    protected ILogger<ResolveFutures> _logger;
    protected IProjection _projection;
    protected ProjectionEventContext _context;
    protected AppendedEvent _event;

    void Establish()
    {
        _projectionFutures = Substitute.For<IProjectionFutures>();
        _projectionFutures.GetFutures().Returns(Task.FromResult<IEnumerable<ProjectionFuture>>([]));
        _tracker = new ProjectionFuturesTracker();
        _typeFormats = Substitute.For<ITypeFormats>();
        _objectComparer = Substitute.For<IObjectComparer>();
        _logger = Substitute.For<ILogger<ResolveFutures>>();
        _step = new ResolveFutures(_projectionFutures, _tracker, _typeFormats, _objectComparer, _logger);

        _projection = Substitute.For<IProjection>();
        _projection.Identifier.Returns(new ProjectionId("test-projection"));
        _projection.TargetReadModelSchema.Returns(new JsonSchema());
        _projection.ChildProjections.Returns([]);
        _projection.ChildrenPropertyPath.Returns(PropertyPath.Root);

        _event = AppendedEvent.EmptyWithEventType(new EventType("TestEvent", EventTypeGeneration.First));

        var changeset = Substitute.For<IChangeset<AppendedEvent, ExpandoObject>>();
        changeset.CurrentState.Returns(new ExpandoObject());

        _context = new ProjectionEventContext(
            new Key("test-key", ArrayIndexers.NoIndexers),
            _event,
            changeset,
            ProjectionOperationType.None,
            NeedsInitialState: false);
    }

    protected ProjectionFuture CreateFuture() => new(
        new ProjectionFutureId(Guid.NewGuid()),
        new ProjectionId("test-projection"),
        _event,
        PropertyPath.NotSet,
        new PropertyPath("NonMatchingChild"),
        PropertyPath.NotSet,
        PropertyPath.NotSet,
        new Key("parent-key", ArrayIndexers.NoIndexers),
        DateTimeOffset.UtcNow);
}
