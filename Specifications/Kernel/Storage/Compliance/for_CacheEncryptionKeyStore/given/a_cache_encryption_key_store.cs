// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Aksio.Cratis.Kernel.Storage.Compliance.for_CacheEncryptionKeyStore.given;

public class a_cache_encryption_key_store : Specification
{
    protected CacheEncryptionKeyStorage store;
    protected Mock<IEncryptionKeyStorage> actual_store;

    void Establish()
    {
        actual_store = new();
        store = new(actual_store.Object);
    }
}
