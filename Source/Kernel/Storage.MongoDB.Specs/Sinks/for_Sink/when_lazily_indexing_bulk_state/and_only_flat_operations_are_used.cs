// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.MongoDB.Sinks.for_Sink.when_lazily_indexing_bulk_state;

public class and_only_flat_operations_are_used : given.a_bulk_sink_with_cached_children
{
    object? _afterWrites;
    object? _afterReset;

    async Task Because()
    {
        await Apply(_first, Root());
        await Apply(_second, Root());
        await _sink.FindOrDefault(_first);
        await Apply(_first, Root());
        await Remove(_second);
        _afterWrites = ChildIndex;
        await _sink.EndBulk();
        await _sink.BeginBulk();
        _afterReset = ChildIndex;
    }

    [Fact] void should_never_activate_for_writes_lends_replacements_or_removals() => _afterWrites.ShouldBeNull();
    [Fact] void should_remain_inactive_in_the_next_window() => _afterReset.ShouldBeNull();
}
