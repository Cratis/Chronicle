// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Confidentiality;

namespace Cratis.Chronicle.ProtectedValues.for_EncryptedMetadataProvider.when_providing_for_type;

public class and_scope_is_global : given.a_provider
{
    [Encrypted(EncryptionScope.Global)]
    record InstallationSigningKey(string Value) : ConceptAs<string>(Value);

    SecurityMetadata _result;

    void Because() => _result = provider.Provide(typeof(InstallationSigningKey));

    [Fact] void should_resolve_to_the_global_metadata_type() => _result.MetadataType.ShouldEqual(SecurityMetadataType.EncryptedGlobal);
}
