// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;

namespace Cratis.Chronicle.Storage.Sql.Sinks.for_LegacyCompositeKey.when_rebuilding;

public class and_the_id_is_unambiguous : given.a_composite_key_schema
{
    bool _result;
    JsonObject? _rebuilt;

    void Because() => _result = _key.TryRebuild($"{FirstPart}_42", out _rebuilt, out _);

    [Fact] void should_rebuild_the_key() => _result.ShouldBeTrue();
    [Fact] void should_rebuild_the_first_part_as_a_guid() => _rebuilt!["first"]!.GetValue<Guid>().ShouldEqual(Guid.Parse(FirstPart));
    [Fact] void should_rebuild_the_second_part_as_a_number() => _rebuilt!["second"]!.GetValue<ulong>().ShouldEqual(42UL);
}
