// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Compliance;

namespace Cratis.Chronicle.Storage.Compliance.for_CacheEncryptionKeyStore.when_checking_if_has_for;

public class and_everything_is_empty : given.a_cache_encryption_key_store
{
    static EncryptionKeyIdentifier identifier = "5c6cce36-d60d-46db-9db2-e820559962db";
    bool result;

    void Establish() => _actualStore.HasFor(string.Empty, string.Empty, identifier).Returns(false);

    async Task Because() => result = await _store.HasFor(string.Empty, string.Empty, identifier);

    [Fact] void should_not_have_key() => result.ShouldBeFalse();
}
