// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Keys;

namespace Cratis.Chronicle.Storage.MongoDB.Sinks.for_Sink.when_lazily_indexing_bulk_state;

public class and_cached_state_changes_after_activation : given.a_bulk_sink_with_cached_children
{
    object? _activated;
    Key? _moved;
    Key? _added;
    Key? _removed;

    async Task Establish()
    {
        await Apply(_first, Root("moving", "removed"));
        await Apply(_second, Root());
        await Find("moving");
        _activated = ChildIndex;
    }

    async Task Because()
    {
        var first = await _sink.FindOrDefault(_first);
        var second = await _sink.FindOrDefault(_second);
        ((IDictionary<string, object?>)first!)["children"] = new List<object>();
        ((IDictionary<string, object?>)second!)["children"] = new List<object> { Child("moving"), Child("added") };
        _moved = await Find("moving");
        _added = await Find("added");
        _removed = await Find("removed");
    }

    [Fact] void should_keep_the_published_instance() => ReferenceEquals(ChildIndex, _activated).ShouldBeTrue();
    [Fact] void should_follow_the_moved_value() => _moved.ShouldEqual(_second);
    [Fact] void should_index_a_previously_nonmatching_root() => _added.ShouldEqual(_second);
    [Fact] void should_not_find_removed_children() => _removed.ShouldBeNull();
}
