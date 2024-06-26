// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Compliance;

namespace Cratis.Chronicle.Storage.Compliance.for_CacheEncryptionKeyStore;

public class when_deleting : given.a_cache_encryption_key_store
{
    static EncryptionKeyIdentifier identifier = "5c6cce36-d60d-46db-9db2-e820559962db";
    static EncryptionKey key = new([], []);

    Task Establish() => store.SaveFor(string.Empty, string.Empty, identifier, key);

    Task Because() => store.DeleteFor(string.Empty, string.Empty, identifier);

    [Fact] void should_delete_key_from_actual_store() => actual_store.Verify(_ => _.DeleteFor(string.Empty, string.Empty, identifier), Once);
    [Fact] async Task should_not_have_the_key_anymore() => (await store.HasFor(string.Empty, string.Empty, identifier)).ShouldBeFalse();
}
