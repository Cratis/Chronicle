// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Projections.Engine.Pipelines.Steps.for_ResolveFutures.when_processing_events_live;

public class and_a_single_child_is_deferred_before_its_root : given.deferred_child_events
{
    IDictionary<string, object?>[] _children;
    int _remaining;

    async Task Because()
    {
        await Process(Event(_childAdded, 1, new { id = "child", name = "Child", email = "initial@example.com" }));
        var context = await Process(Event(_rootCreated, 2, new { name = "Root" }));
        _children = await StoredChildren(context.Key);
        _remaining = (await _futures.GetFutures()).Count();
    }

    [Fact] void should_store_one_child() => _children.Length.ShouldEqual(1);
    [Fact] void should_preserve_the_identifier() => _children[0]["id"].ShouldEqual("child");
    [Fact] void should_preserve_the_name() => _children[0]["name"].ShouldEqual("Child");
    [Fact] void should_preserve_the_email() => _children[0]["email"].ShouldEqual("initial@example.com");
    [Fact] void should_acknowledge_the_saved_future() => _remaining.ShouldEqual(0);
}
