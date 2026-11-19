// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;

namespace Cratis.Chronicle.Storage.MongoDB.Sinks.for_Sink.when_upserting_guarded;

public class and_an_ordered_write_replaces_a_cached_state : when_applying_changes_in_order.given.a_bulk_sink_recording_writes
{
    ExpandoObject _cached;
    ExpandoObject _after;

    async Task Establish() => _cached = (await SeedAndReadCachedState())!;

    async Task Because()
    {
        await ApplyGuarded();
        _after = (await _sink.FindOrDefault(RootKey))!;
        await _sink.EndBulk();
    }

    [Fact] void should_start_with_a_cached_state() => ValueOf(_cached).ShouldEqual(5);
    [Fact] void should_read_the_new_value_instead_of_the_cached_state() => ValueOf(_after).ShouldEqual(20);
    [Fact] void should_flush_the_ordered_direct_run_through_bulk() => _batches.Count.ShouldEqual(2);
    [Fact] void should_preserve_the_direct_then_join_order() => _orderedWrites.ShouldEqual(["direct:5", "direct:10", "join:20"]);
}
