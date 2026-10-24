// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.Sql.Sinks.for_LegacyCompositeKey.when_rebuilding;

public class and_a_part_is_empty : given.a_composite_key_schema
{
    bool _emptyFirst;
    bool _emptySecond;
    string? _reason;

    void Because()
    {
        _emptyFirst = _key.TryRebuild("_42", out _, out _reason);
        _emptySecond = _key.TryRebuild($"{FirstPart}_", out _, out _);
    }

    [Fact] void should_not_rebuild_when_the_first_part_is_empty() => _emptyFirst.ShouldBeFalse();
    [Fact] void should_not_rebuild_when_the_second_part_is_empty() => _emptySecond.ShouldBeFalse();
    [Fact] void should_say_a_part_is_empty() => _reason.ShouldEqual("a key part is empty");
}
