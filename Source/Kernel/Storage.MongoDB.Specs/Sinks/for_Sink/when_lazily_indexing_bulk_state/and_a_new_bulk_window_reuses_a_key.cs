// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Keys;

namespace Cratis.Chronicle.Storage.MongoDB.Sinks.for_Sink.when_lazily_indexing_bulk_state;

public class and_a_new_bulk_window_reuses_a_key : given.a_bulk_sink_with_cached_children
{
    object? _previous;
    object? _reset;
    Key? _old;
    Key? _current;

    async Task Establish()
    {
        await Apply(_first, Root("old"));
        await Find("old");
        _previous = ChildIndex;
    }

    async Task Because()
    {
        await _sink.EndBulk();
        await _sink.BeginBulk();
        await Apply(_first, Root("current"));
        _reset = ChildIndex;
        _old = await Find("old");
        _current = await Find("current");
    }

    [Fact] void should_detach_the_old_index() => _reset.ShouldBeNull();
    [Fact] void should_build_a_new_instance() => ReferenceEquals(ChildIndex, _previous).ShouldBeFalse();
    [Fact] void should_not_reuse_old_values() => _old.ShouldBeNull();
    [Fact] void should_find_the_reused_key() => _current.ShouldEqual(_first);
}
