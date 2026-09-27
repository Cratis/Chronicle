// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using System.Text.Json;
using Cratis.Chronicle.Changes;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Properties;
using Cratis.Chronicle.Schemas;

namespace Cratis.Chronicle.Projections.Engine.Pipelines.Steps.for_ResolveFutures.when_processing_events_live;

public class and_a_child_arrives_before_its_root : given.a_first_level_child_future
{
    bool _resolvedBeforeRoot;
    bool _matchesReplayOrder;

    void Establish()
    {
        var schema = new JsonSchema();
        schema.Properties["id"] = new JsonSchemaProperty("id", new System.Text.Json.Nodes.JsonObject { ["type"] = "string" }, schema);
        _projection.TargetReadModelSchema.Returns(schema);
        SetFutureKey("root-key");
    }

    async Task Because()
    {
        await _step.Perform(_projection, _context with { Key = new Key("root-key", ArrayIndexers.NoIndexers) });
        _resolvedBeforeRoot = _resolved;
        await ProcessRoot(RootWith("id", "root-key", initialized: null), initializeNow: true);

        // In replay order the root already exists when its child is projected.
        var replay = new Changeset<AppendedEvent, ExpandoObject>(new ObjectComparer(), _event, RootWith("id", "root-key", initialized: null));
        replay.AddChild<ExpandoObject>(
            new PropertyPath("children"),
            new PropertyPath("childId"),
            "child-key",
            [],
            new ArrayIndexers([new ArrayIndexer(new PropertyPath("children"), new PropertyPath("childId"), "child-key")]));
        _matchesReplayOrder = JsonSerializer.Serialize(_result!.PendingFutureSaves.Single().Changeset.CurrentState) ==
            JsonSerializer.Serialize(replay.CurrentState);
    }

    [Fact] void should_leave_the_future_pending_before_the_root_arrives() => _resolvedBeforeRoot.ShouldBeFalse();
    [Fact] void should_resolve_the_future_after_the_root_arrives() => _projectionFutures.Received(1).ResolveFuture(_future.Id);
    [Fact] void should_project_the_child_under_the_root() => HasChild.ShouldBeTrue();
    [Fact] void should_produce_the_same_state_as_replay_order() => _matchesReplayOrder.ShouldBeTrue();
    [Fact] void should_save_the_child_under_the_root_key() => _result!.PendingFutureSaves.Single().Key.Value.ShouldEqual("root-key");
    [Fact] void should_use_only_the_child_indexer() => _result!.PendingFutureSaves.Single().Key.ArrayIndexers.All.Single().ArrayProperty.ShouldEqual(new PropertyPath("children"));
    [Fact] void should_leave_no_future_pending() => _tracker.HasPending.ShouldBeFalse();
}
