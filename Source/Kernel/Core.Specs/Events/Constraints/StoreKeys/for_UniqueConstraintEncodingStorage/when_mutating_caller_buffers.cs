// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events.Constraints;
using Cratis.Chronicle.Storage.Events.Constraints;

namespace Cratis.Chronicle.Events.Constraints.StoreKeys.for_UniqueConstraintEncodingStorage;

public class when_mutating_caller_buffers : given.a_key_storage
{
    byte[] _input;
    UniqueConstraintEncodingKey _stored;

    void Establish() => _input = Enumerable.Repeat((byte)42, UniqueConstraintStoreKey.Size).ToArray();

    async Task Because()
    {
        var key = new UniqueConstraintEncodingKey(new UniqueConstraintStoreKeyId("00112233445566778899aabbccddeeff"), new UniqueConstraintStoreKey(_input));
        await _storage.CreateKeyIfAbsent(key);
        Array.Fill(_input, (byte)0);
        var read = (await _storage.GetKey())!;
        var output = read.Material.CopyBytes();
        Array.Fill(output, (byte)1);
        _stored = (await _storage.GetKey())!;
    }

    [Fact] void should_preserve_every_byte() => _stored.Material.CopyBytes().All(value => value == 42).ShouldBeTrue();
    [Fact] void should_preserve_the_key_size() => _stored.Material.CopyBytes().Length.ShouldEqual(UniqueConstraintStoreKey.Size);
}
