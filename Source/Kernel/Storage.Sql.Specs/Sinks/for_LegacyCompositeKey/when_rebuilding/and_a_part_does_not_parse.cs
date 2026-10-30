// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.Sql.Sinks.for_LegacyCompositeKey.when_rebuilding;

public class and_a_part_does_not_parse : given.a_composite_key_schema
{
    bool _result;
    string? _reason;
    Exception? _error;

    void Because() => _error = Catch.Exception(() => _result = _key.TryRebuild($"{FirstPart}_abc", out _, out _reason));

    [Fact] void should_not_throw() => _error.ShouldBeNull();
    [Fact] void should_not_rebuild_the_key() => _result.ShouldBeFalse();
    [Fact] void should_say_the_part_does_not_parse() => _reason.ShouldEqual("a key part does not parse as its schema type");
}
