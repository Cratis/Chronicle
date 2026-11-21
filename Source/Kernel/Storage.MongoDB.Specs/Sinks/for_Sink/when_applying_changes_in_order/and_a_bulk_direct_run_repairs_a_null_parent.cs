// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using Cratis.Chronicle.Changes;
using MongoDB.Bson;

namespace Cratis.Chronicle.Storage.MongoDB.Sinks.for_Sink.when_applying_changes_in_order;

public class and_a_bulk_direct_run_repairs_a_null_parent : given.a_bulk_sink_recording_writes
{
    async Task Establish()
    {
        _documents[_key]["info"] = BsonNull.Value;
        _changes =
        [
            new PropertiesChanged<ExpandoObject>(new ExpandoObject(),
            [
                new PropertyDifference("value", 0, 10),
                new PropertyDifference("info.name", null, "recreated")
            ]),
            Join(20)
        ];
        await _sink.BeginBulk();
    }

    async Task Because()
    {
        await Apply();
        await _sink.EndBulk();
    }

    [Fact] void should_repair_before_the_direct_write_and_the_following_join() => _orderedWrites.ShouldEqual(["repair", "direct:10", "join:20"]);
    [Fact] void should_flush_the_repair_together_with_the_direct_run() => _batches.Single().Length.ShouldEqual(2);
}
