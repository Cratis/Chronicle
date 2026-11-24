// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Properties;

namespace Cratis.Chronicle.Setup.Serialization.for_KernelSerializers;

public class when_round_tripping_keys_of_each_kind : given.the_kernel_serializers
{
    static readonly Guid _guid = new("6d884b84-f045-4df5-b6d7-f56fa2f7a953");
    Key _guidKey;
    Key _intKey;
    Key _longKey;
    Key _stringKey;
    Key _indexedKey;
    Key _undefined;

    void Because()
    {
        _guidKey = (Key)SendBetweenSilos(typeof(Key), new Key(_guid, ArrayIndexers.NoIndexers))!;
        _intKey = (Key)SendBetweenSilos(typeof(Key), new Key(42, ArrayIndexers.NoIndexers))!;
        _longKey = (Key)SendBetweenSilos(typeof(Key), new Key(42L, ArrayIndexers.NoIndexers))!;
        _stringKey = (Key)SendBetweenSilos(typeof(Key), new Key("some-key", ArrayIndexers.NoIndexers))!;
        _indexedKey = (Key)SendBetweenSilos(typeof(Key), new Key("some-key", new ArrayIndexers([new ArrayIndexer("items", "id", _guid)])))!;
        _undefined = (Key)SendBetweenSilos(typeof(Key), Key.Undefined)!;
    }

    [Fact] void should_keep_a_guid_value() => _guidKey.Value.ShouldEqual(_guid);
    [Fact] void should_keep_an_int_value() => _intKey.Value.ShouldEqual(42);
    [Fact] void should_keep_a_long_value() => _longKey.Value.ShouldEqual(42L);
    [Fact] void should_keep_a_string_value() => _stringKey.Value.ShouldEqual("some-key");
    [Fact] void should_be_equal_to_the_key_that_was_sent() => _stringKey.ShouldEqual(new Key("some-key", ArrayIndexers.NoIndexers));
    [Fact] void should_share_the_empty_array_indexers() => ReferenceEquals(_stringKey.ArrayIndexers, ArrayIndexers.NoIndexers).ShouldBeTrue();
    [Fact] void should_keep_the_array_indexers() => _indexedKey.ArrayIndexers.GetFor("items").Identifier.ShouldEqual(_guid);
    [Fact] void should_keep_an_undefined_key_undefined() => _undefined.Value.ShouldBeNull();
}
