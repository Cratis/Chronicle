// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Compliance;

namespace Cratis.Chronicle.Storage.Compliance.for_CacheEncryptionKeyStore.given;

public class a_cache_with_present_keys : a_cache_encryption_key_store
{
    protected readonly EncryptionKey _key = new([1], [2]);
    protected EncryptionKeyAtRevision _pair;

    void Establish()
    {
        _pair = new(_key, EncryptionKeyRevision.Initial);
        _actualStore.TryGetFor("store", "ns", Arg.Any<EncryptionKeyIdentifier>(), Arg.Any<EncryptionKeyRevision?>()).Returns(_key);
        _actualStore.TryGetLatestFor("store", "ns", Arg.Any<EncryptionKeyIdentifier>()).Returns(_pair);
        _actualStore.TryGetAtRevisionFor("store", "ns", Arg.Any<EncryptionKeyIdentifier>(), Arg.Any<EncryptionKeyRevision>())
            .Returns(call => new EncryptionKeyAtRevision(_key, call.Arg<EncryptionKeyRevision>()));
    }
}
