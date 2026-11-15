// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.ProtectedValues.for_EncryptedNamespaceValueHandler;

public class when_erasure_happens_behind_a_warm_cache : ProtectedValues.given.an_erased_key_behind_a_warm_cache
{
    async Task Establish()
    {
        _handler = new EncryptedNamespaceValueHandler(_provisioner, _cache, _encryption);
        await ProtectAndErase("store", "ns", EncryptedValueKeyIdentifiers.ForNamespace());
    }

    Task Because() => ReleaseValues();

    [Fact] void should_release_empty() => _result.ToString().ShouldEqual(string.Empty);
    [Fact] void should_not_try_release_the_erased_value() => _tried.ShouldBeNull();
}
