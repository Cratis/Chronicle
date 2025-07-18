// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Compliance;

namespace Cratis.Chronicle.Storage.Compliance.for_CacheEncryptionKeyStore;

public class when_saving : given.a_cache_encryption_key_store
{
    static EncryptionKeyIdentifier _identifier = "5c6cce36-d60d-46db-9db2-e820559962db";
    static EncryptionKey _key = new([], []);

    Task Because() => _store.SaveFor(string.Empty, string.Empty, _identifier, _key);

    [Fact] void should_save_key_to_actual_store() => _actualStore.Received(1).SaveFor(string.Empty, string.Empty, _identifier, _key);
    [Fact] async Task should_have_the_key_anymore() => (await _store.HasFor(string.Empty, string.Empty, _identifier)).ShouldBeTrue();
}
