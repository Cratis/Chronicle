// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;

namespace Cratis.Chronicle.Storage.Sql.Sinks.for_LegacyCompositeKey.when_creating;

public class and_a_part_is_an_object : given.a_composite_key_schema
{
    bool _result;
    JsonObject? _rebuilt;
    string? _reason;

    void Because()
    {
        var key = CreateFor("""{"type":"object","properties":{"id":{"type":"object","properties":{"first":{"type":"string"},"nested":{"type":"object","properties":{"value":{"type":"string"}}}}}}}""")!;
        _result = key.TryRebuild("a_b", out _rebuilt, out _reason);
    }

    [Fact] void should_not_rebuild_the_key() => _result.ShouldBeFalse();
    [Fact] void should_not_return_a_key() => _rebuilt.ShouldBeNull();
    [Fact] void should_say_the_part_is_unsupported() => _reason.ShouldEqual("a key part is an object or collection");
}
