// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Compliance;

namespace Cratis.Chronicle.Storage.Compliance.for_CacheEncryptionKeyStore;

public class when_deleting : given.a_cache_encryption_key_store
{
    static EncryptionKeyIdentifier identifier = "5c6cce36-d60d-46db-9db2-e820559962db";
    static EncryptionKey key = new([], []);

    Task Establish() => _store.SaveFor(string.Empty, string.Empty, identifier, key);

    Task Because() => _store.DeleteFor(string.Empty, string.Empty, identifier);

    [Fact] void should_delete_key_from_actual_store() => _actualStore.Received(1).DeleteFor(string.Empty, string.Empty, identifier);
    [Fact] async Task should_not_have_the_key_anymore() => (await _store.HasFor(string.Empty, string.Empty, identifier)).ShouldBeFalse();
}
