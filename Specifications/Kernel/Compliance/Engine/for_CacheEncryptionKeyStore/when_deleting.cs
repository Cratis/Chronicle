// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Aksio.Cratis.Compliance.Engine.for_CacheEncryptionKeyStore;

public class when_deleting : given.a_cache_encryption_key_store
{
    static EncryptionKeyIdentifier identifier = "5c6cce36-d60d-46db-9db2-e820559962db";
    static EncryptionKey key = new(Array.Empty<byte>(), Array.Empty<byte>());

    Task Establish() => store.SaveFor(identifier, key);

    Task Because() => store.DeleteFor(identifier);

    [Fact] void should_delete_key_from_actual_store() => actual_store.Verify(_ => _.DeleteFor(identifier), Once());
    [Fact] async Task should_not_have_the_key_anymore() => (await store.HasFor(identifier)).ShouldBeFalse();
}
