// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using Cratis.Chronicle.Changes;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Concepts.Projections;
using Cratis.Chronicle.Properties;
using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle.Projections.Engine.Pipelines.Steps.for_StoreFutures.given;

public class a_store_futures_step : Specification
{
    protected StoreFutures _step;
    protected IProjectionFutures _projectionFutures;
    protected ProjectionFuturesTracker _tracker;
    protected ILogger<StoreFutures> _logger;
    protected IProjection _projection;
    protected ProjectionEventContext _context;
    protected AppendedEvent _event;

    void Establish()
    {
        _projectionFutures = Substitute.For<IProjectionFutures>();
        _projectionFutures.AddFuture(Arg.Any<ProjectionFuture>()).Returns(Task.FromResult(1));
        _tracker = new ProjectionFuturesTracker { HasPending = false };
        _logger = Substitute.For<ILogger<StoreFutures>>();
        _step = new StoreFutures(_projectionFutures, _tracker, _logger);

        _projection = Substitute.For<IProjection>();
        _event = AppendedEvent.EmptyWithEventType(new EventType("TestEvent", EventTypeGeneration.First));

        var changeset = Substitute.For<IChangeset<AppendedEvent, ExpandoObject>>();
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
        new PropertyPath("Children"),
        PropertyPath.NotSet,
        PropertyPath.NotSet,
        new Key("parent-key", ArrayIndexers.NoIndexers),
        DateTimeOffset.UtcNow);
}
