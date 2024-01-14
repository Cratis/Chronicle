// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Aksio.Cratis.Compliance;

namespace Aksio.Cratis.Kernel.Storage.Compliance.for_CacheEncryptionKeyStore.when_checking_if_has_for;

public class and_cache_has_it : given.a_cache_encryption_key_store
{
    static EncryptionKeyIdentifier identifier = "5c6cce36-d60d-46db-9db2-e820559962db";
    bool result;

    Task Establish() => store.SaveFor(string.Empty, string.Empty, identifier, new(Array.Empty<byte>(), Array.Empty<byte>()));

    async Task Because() => result = await store.HasFor(string.Empty, string.Empty, identifier);

    [Fact] void should_not_ask_actual_store() => actual_store.Verify(_ => _.HasFor(string.Empty, string.Empty, identifier), Never);
    [Fact] void should_have_it_in_cache() => result.ShouldBeTrue();
}
