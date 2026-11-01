// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Keys;

namespace Cratis.Chronicle.Storage.MongoDB.Sinks.for_Sink.when_lazily_indexing_bulk_state;

public class and_a_detached_write_fails : given.a_bulk_sink_with_cached_children
{
    Key? _after;
    int _failures;

    async Task Establish()
    {
        await Apply(_first, Root("failed"));
        await Find("failed");
        _failWrites = true;
    }

    async Task Because()
    {
        var result = await _sink.FlushBulk();
        _failures = result.FailedPartitions.Count();
        _after = await Find("failed");
    }

    [Fact] void should_observe_the_failed_partition() => _failures.ShouldEqual(1);
    [Fact] void should_not_resurrect_the_failed_cached_write() => _after.ShouldBeNull();
}
