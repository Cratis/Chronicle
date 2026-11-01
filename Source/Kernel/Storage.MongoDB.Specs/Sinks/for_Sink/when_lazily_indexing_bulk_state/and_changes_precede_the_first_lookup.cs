// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Keys;

namespace Cratis.Chronicle.Storage.MongoDB.Sinks.for_Sink.when_lazily_indexing_bulk_state;

public class and_changes_precede_the_first_lookup : given.a_bulk_sink_with_cached_children
{
    object? _beforeLookup;
    Key? _final;
    Key? _old;
    Key? _removed;

    async Task Establish()
    {
        await Apply(_first, Root("old"));
        await Apply(_second, Root("removed"));
        var lent = await _sink.FindOrDefault(_first);
        ((IDictionary<string, object?>)lent!)["children"] = new List<object> { Child("lent") };
        await Apply(_first, Root("final"));
        await Remove(_second);
        _beforeLookup = ChildIndex;
    }

    async Task Because()
    {
        _final = await Find("final");
        _old = await Find("old");
        _removed = await Find("removed");
    }

    [Fact] void should_not_record_inactive_history() => _beforeLookup.ShouldBeNull();
    [Fact] void should_activate_on_the_first_lookup() => ChildIndex.ShouldNotBeNull();
    [Fact] void should_find_the_final_state() => _final.ShouldEqual(_first);
    [Fact] void should_not_find_replaced_values() => _old.ShouldBeNull();
    [Fact] void should_not_find_removed_roots() => _removed.ShouldBeNull();
}
