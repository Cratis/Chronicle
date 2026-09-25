// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using System.Text.Json.Nodes;
using Cratis.Chronicle.Changes;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Concepts.Projections;
using Cratis.Chronicle.Properties;
using Cratis.Chronicle.Schemas;

namespace Cratis.Chronicle.Projections.Engine.Pipelines.Steps.for_ResolveFutures.when_processing_events_live;

public class and_a_child_arrives_before_its_root : given.a_resolve_futures_step
{
    ProjectionFuture _future;
    IProjection _child;
    ProjectionEventContext? _resolvedContext;
    bool _resolved;
    bool _resolvedBeforeRoot;

    void Establish()
    {
        var schema = new JsonSchema();
        schema.Properties["name"] = new JsonSchemaProperty("name", new JsonObject { ["type"] = "string" }, schema);
        _projection.TargetReadModelSchema.Returns(schema);

        _child = Substitute.For<IProjection>();
        _child.Parent.Returns(_projection);
        _child.Path.Returns(new ProjectionPath("children"));
        _child.ChildrenPropertyPath.Returns(new PropertyPath("children"));
        _child.OnNext(Arg.Do<ProjectionEventContext>(context => _resolvedContext = context));
        _projection.ChildProjections.Returns([_child]);

        _future = new ProjectionFuture(
            ProjectionFutureId.New(),
            _projection.Identifier,
            _event,
            PropertyPath.Root,
            new PropertyPath("children"),
            new PropertyPath("childId"),
            new PropertyPath("childId"),
            new Key("root-key", ArrayIndexers.NoIndexers),
            DateTimeOffset.UtcNow);
        _projectionFutures.GetFutures().Returns(_ => Task.FromResult<IEnumerable<ProjectionFuture>>(_resolved ? [] : [_future]));
        _projectionFutures.ResolveFuture(_future.Id).Returns(_ =>
        {
            _resolved = true;
            return Task.CompletedTask;
        });
        _tracker.HasPending = true;
        _context = _context with { Key = new Key("root-key", ArrayIndexers.NoIndexers) };
    }

    async Task Because()
    {
        // Live processing sees the child first, with no root document to attach it to.
        await _step.Perform(_projection, _context);
        _resolvedBeforeRoot = _resolved;

        dynamic rootState = new ExpandoObject();
        rootState.id = "root-key";
        rootState.children = new List<ExpandoObject>();
        var rootChangeset = Substitute.For<IChangeset<AppendedEvent, ExpandoObject>>();
        rootChangeset.CurrentState.Returns((ExpandoObject)rootState);
        var rootEvent = AppendedEvent.EmptyWithEventType(new EventType("RootCreated", EventTypeGeneration.First));
        await _step.Perform(_projection, _context with { Event = rootEvent, Changeset = rootChangeset });
    }

    [Fact] void should_leave_the_future_pending_before_the_root_arrives() => _resolvedBeforeRoot.ShouldBeFalse();
    [Fact] void should_resolve_the_future_after_the_root_arrives() => _projectionFutures.Received(1).ResolveFuture(_future.Id);
    [Fact] void should_project_the_child_under_the_root() => _resolvedContext!.Key.Value.ShouldEqual("root-key");
    [Fact] void should_use_only_the_child_indexer() => _resolvedContext!.Key.ArrayIndexers.All.Single().ArrayProperty.ShouldEqual(new PropertyPath("children"));
    [Fact] void should_leave_no_future_pending() => _tracker.HasPending.ShouldBeFalse();
}
