// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;

namespace Cratis.Chronicle.Storage.Sql.Sinks.for_LegacyCompositeKey.when_rebuilding;

public class and_a_part_contains_the_separator : given.a_composite_key_schema
{
    bool _result;
    JsonObject? _rebuilt;
    string? _reason;

    void Because() => _result = _key.TryRebuild("a_b_42", out _rebuilt, out _reason);

    [Fact] void should_not_rebuild_the_key() => _result.ShouldBeFalse();
    [Fact] void should_not_return_a_key() => _rebuilt.ShouldBeNull();
    [Fact] void should_say_a_part_contains_the_separator_or_is_missing() => _reason.ShouldEqual("a key part contains '_' or is missing");
}
