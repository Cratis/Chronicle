// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;

namespace Cratis.Chronicle.Storage.Sql.Sinks.for_LegacyCompositeKey.when_rebuilding;

public class and_a_part_references_an_unsigned_integer : given.a_composite_key_schema
{
    bool _result;
    JsonObject? _rebuilt;

    void Establish() => _key = CreateFor("""{"type":"object","definitions":{"unsignedInteger":{"type":"integer","format":"uint64"}},"properties":{"id":{"type":"object","properties":{"first":{"type":"string","format":"guid"},"second":{"$ref":"#/definitions/unsignedInteger"}}}}}""")!;

    void Because() => _result = _key.TryRebuild($"{FirstPart}_{ulong.MaxValue}", out _rebuilt, out _);

    [Fact] void should_rebuild_the_key() => _result.ShouldBeTrue();
    [Fact] void should_rebuild_the_referenced_part_as_an_unsigned_integer() => _rebuilt!["second"]!.GetValue<ulong>().ShouldEqual(ulong.MaxValue);
}
