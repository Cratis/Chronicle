// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.Sql.Sinks.for_LegacyCompositeKey.when_rebuilding;

public class and_a_part_does_not_round_trip : given.a_composite_key_schema
{
    bool _result;
    string? _reason;

    void Because() => _result = _key.TryRebuild($"{FirstPart}_042", out _, out _reason);

    [Fact] void should_not_rebuild_the_key() => _result.ShouldBeFalse();
    [Fact] void should_say_the_part_does_not_round_trip() => _reason.ShouldEqual("a key part does not round-trip");
}
