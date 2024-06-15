// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.Compliance.for_CacheEncryptionKeyStore;

public class when_saving : given.a_cache_encryption_key_store
{
    static EncryptionKeyIdentifier identifier = "5c6cce36-d60d-46db-9db2-e820559962db";
    static EncryptionKey key = new(Array.Empty<byte>(), Array.Empty<byte>());

    Task Because() => store.SaveFor(string.Empty, string.Empty, identifier, key);

    [Fact] void should_save_key_to_actual_store() => actual_store.Verify(_ => _.SaveFor(string.Empty, string.Empty, identifier, key), Once);
    [Fact] async Task should_have_the_key_anymore() => (await store.HasFor(string.Empty, string.Empty, identifier)).ShouldBeTrue();
}
