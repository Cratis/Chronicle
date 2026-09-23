// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.ProtectedValues.for_EncryptedMetadataProvider.when_providing_for_type;

public class and_scope_is_global : given.a_provider
{
    [Encrypted(EncryptionScope.Global)]
    record InstallationSigningKey(string Value) : ConceptAs<string>(Value);

    Exception _result;

    void Because() => _result = Catch.Exception(() => provider.Provide(typeof(InstallationSigningKey)));

    [Fact] void should_throw_encryption_scope_not_yet_supported() => _result.ShouldBeOfExactType<EncryptionScopeNotYetSupported>();
}
