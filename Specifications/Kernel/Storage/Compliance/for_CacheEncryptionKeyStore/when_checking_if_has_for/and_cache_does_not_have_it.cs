// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Aksio.Cratis.Compliance;

namespace Aksio.Cratis.Kernel.Storage.Compliance.for_CacheEncryptionKeyStore.when_checking_if_has_for;

public class and_cache_does_not_have_it : given.a_cache_encryption_key_store
{
    static EncryptionKeyIdentifier identifier = "5c6cce36-d60d-46db-9db2-e820559962db";
    bool result;

    void Establish() => actual_store.Setup(_ => _.HasFor(string.Empty, string.Empty, identifier)).Returns(Task.FromResult(true));

    async Task Because() => result = await store.HasFor(string.Empty, string.Empty, identifier);

    [Fact] void should_ask_actual_store() => actual_store.Verify(_ => _.HasFor(string.Empty, string.Empty, identifier), Once);
    [Fact] void should_return_result_from_actual_store() => result.ShouldBeTrue();
}
