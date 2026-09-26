// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using Cratis.Chronicle.Properties;

namespace Cratis.Chronicle.Projections.Engine.Pipelines.Steps.for_ResolveFutures.when_processing_events_live;

public class and_a_child_update_arrives_before_its_creation : given.a_first_level_child_future
{
    bool _resolvedBeforeChild;
    ProjectionEventContext? _resolvedUpdate;

    void Establish()
    {
        SetFutureKey("child-key");
        _projection.ChildProjections.First().OnNext(Arg.Do<ProjectionEventContext>(context => _resolvedUpdate = context));
    }

    async Task Because()
    {
        await ProcessRoot(RootWith("id", "root-key"), key: "root-key");
        _resolvedBeforeChild = _resolved;

        var root = RootWith("id", "root-key");
        var child = new ExpandoObject();
        ((IDictionary<string, object?>)child)["childId"] = "child-key";
        ((IDictionary<string, object?>)root)["children"] = new List<ExpandoObject> { child };
        await ProcessRoot(root, key: "root-key");
    }

    [Fact] void should_not_attach_before_the_child_exists() => _resolvedBeforeChild.ShouldBeFalse();
    [Fact] void should_resolve_after_the_child_is_added_under_the_root() => _projectionFutures.Received(1).ResolveFuture(_future.Id);
    [Fact] void should_use_the_root_key_for_the_child_update() => _resolvedUpdate!.Key.Value.ShouldEqual("root-key");
    [Fact] void should_use_only_the_child_indexer() => _resolvedUpdate!.Key.ArrayIndexers.All.Single().ArrayProperty.ShouldEqual(new PropertyPath("children"));
}
