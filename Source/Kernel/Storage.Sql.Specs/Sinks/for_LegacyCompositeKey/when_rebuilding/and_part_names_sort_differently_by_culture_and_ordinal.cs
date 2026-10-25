// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;

namespace Cratis.Chronicle.Storage.Sql.Sinks.for_LegacyCompositeKey.when_rebuilding;

public class and_part_names_sort_differently_by_culture_and_ordinal : given.a_composite_key_schema
{
    bool _result;
    JsonObject? _rebuilt;

    void Because()
    {
        var key = CreateFor("""{"type":"object","properties":{"id":{"type":"object","properties":{"B":{"type":"string"},"a":{"type":"string"}}}}}""")!;
        _result = key.TryRebuild("x_y", out _rebuilt, out _);
    }

    [Fact] void should_rebuild_the_key() => _result.ShouldBeTrue();
    [Fact] void should_order_the_parts_the_way_the_id_is_written() => _rebuilt!["a"]!.GetValue<string>().ShouldEqual("x");
    [Fact] void should_assign_the_remaining_part() => _rebuilt!["B"]!.GetValue<string>().ShouldEqual("y");
}
