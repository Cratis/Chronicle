// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.Sql.Sinks.for_LegacyCompositeKey.when_rebuilding;

public class and_a_part_is_missing : given.a_composite_key_schema
{
    bool _result;
    string? _reason;

    void Because() => _result = _key.TryRebuild(FirstPart, out _, out _reason);

    [Fact] void should_not_rebuild_the_key() => _result.ShouldBeFalse();
    [Fact] void should_say_a_part_contains_the_separator_or_is_missing() => _reason.ShouldEqual("a key part contains '_' or is missing");
}
