// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.Sql.Sinks.for_LegacyCompositeKey.when_creating;

public class and_the_key_is_a_scalar : given.a_composite_key_schema
{
    LegacyCompositeKey? _guidKey;
    LegacyCompositeKey? _stringKey;
    LegacyCompositeKey? _synthesizedKey;

    void Because()
    {
        _guidKey = CreateFor("""{"type":"object","properties":{"id":{"type":"string","format":"guid"},"name":{"type":"string"}}}""");
        _stringKey = CreateFor("""{"type":"object","properties":{"id":{"type":"string"},"name":{"type":"string"}}}""");
        _synthesizedKey = CreateFor("""{"type":"object","properties":{"name":{"type":"string"}}}""");
    }

    [Fact] void should_not_create_for_a_guid_key() => _guidKey.ShouldBeNull();
    [Fact] void should_not_create_for_a_string_key() => _stringKey.ShouldBeNull();
    [Fact] void should_not_create_for_a_synthesized_key() => _synthesizedKey.ShouldBeNull();
}
