// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Events.Constraints;

namespace Cratis.Chronicle.Events.Constraints.StoreKeys.for_UniqueConstraintEncodingKey;

public class when_creating_keys : Specification
{
    UniqueConstraintEncodingKey _first;
    UniqueConstraintEncodingKey _second;

    void Because()
    {
        _first = UniqueConstraintEncodingKey.Create();
        _second = UniqueConstraintEncodingKey.Create();
    }

    [Fact] void should_create_a_256_bit_key() => _first.Material.CopyBytes().Length.ShouldEqual(32);
    [Fact] void should_create_a_128_bit_lowercase_hexadecimal_identity() => Assert.Matches("^[0-9a-f]{32}$", _first.KeyId.Value);
    [Fact] void should_create_fresh_identities() => (_first.KeyId != _second.KeyId).ShouldBeTrue();
    [Fact] void should_create_fresh_material() => _first.Material.CopyBytes().SequenceEqual(_second.Material.CopyBytes()).ShouldBeFalse();
}
