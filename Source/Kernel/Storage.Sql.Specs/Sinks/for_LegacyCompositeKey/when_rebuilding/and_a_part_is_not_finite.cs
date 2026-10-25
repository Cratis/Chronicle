// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;

namespace Cratis.Chronicle.Storage.Sql.Sinks.for_LegacyCompositeKey.when_rebuilding;

public class and_a_part_is_not_finite : given.a_composite_key_schema
{
    LegacyCompositeKey[] _keys;
    (bool Rebuilt, JsonObject? Key, string? Reason)[] _results;
    Exception? _error;

    void Establish() => _keys =
    [
        CreateFor("""{"type":"object","properties":{"id":{"type":"object","properties":{"first":{"type":"number","format":"double"},"second":{"type":"integer"}}}}}""")!,
        CreateFor("""{"type":"object","properties":{"id":{"type":"object","properties":{"first":{"type":"number","format":"float"},"second":{"type":"integer"}}}}}""")!
    ];

    void Because() => _error = Catch.Exception(() => _results = _keys.SelectMany(key =>
        new[] { double.NaN, double.PositiveInfinity, double.NegativeInfinity }.Select(value =>
        {
            var rebuilt = key.TryRebuild($"{value}_1", out var result, out var reason);
            return (rebuilt, result, reason);
        })).ToArray());

    [Fact] void should_not_throw() => _error.ShouldBeNull();
    [Fact] void should_reject_all_non_finite_parts() => _results.All(result => !result.Rebuilt).ShouldBeTrue();
    [Fact] void should_leave_all_keys_unset() => _results.All(result => result.Key is null).ShouldBeTrue();
    [Fact] void should_give_a_fixed_reason_without_key_values() => _results.All(result => result.Reason == "a key part is not finite").ShouldBeTrue();
    [Fact] void should_check_both_floating_point_types_and_all_non_finite_values() => _results.Length.ShouldEqual(6);
}
