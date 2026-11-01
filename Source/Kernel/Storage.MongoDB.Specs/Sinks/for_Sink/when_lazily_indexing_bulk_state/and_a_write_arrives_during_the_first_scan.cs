// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Keys;

namespace Cratis.Chronicle.Storage.MongoDB.Sinks.for_Sink.when_lazily_indexing_bulk_state;

public class and_a_write_arrives_during_the_first_scan : given.a_paused_first_lookup
{
    Key? _current;
    Key? _old;

    async Task Because()
    {
        try
        {
            ChildIndex.ShouldNotBeNull();
            await Apply(_first, Root("current"));
        }
        finally
        {
            _releaseScan.TrySetResult();
        }

        await _lookup.WaitAsync(Deadline);
        _current = await Find("current");
        _old = await Find("old");
    }

    [Fact] void should_preserve_the_dirty_mark_from_the_scan() => _current.ShouldEqual(_first);
    [Fact] void should_not_keep_the_previous_value() => _old.ShouldBeNull();
}
