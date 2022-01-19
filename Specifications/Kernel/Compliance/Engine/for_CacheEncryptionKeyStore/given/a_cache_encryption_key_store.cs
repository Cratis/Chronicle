// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Compliance.Engine.for_CacheEncryptionKeyStore.given
{
    public class a_cache_encryption_key_store : Specification
    {
        protected CacheEncryptionKeyStore   store;
        protected Mock<IEncryptionKeyStore> actual_store;

        void Establish()
        {
            actual_store = new();
            store = new(actual_store.Object);
        }
    }
}
