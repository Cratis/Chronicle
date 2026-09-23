// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Confidentiality;

namespace Cratis.Chronicle.ProtectedValues.for_EncryptedMetadataProvider.when_providing_for_type;

public class and_scope_is_namespace : given.a_provider
{
    [Encrypted(EncryptionScope.Namespace)]
    record PartnerWebhookSecret(string Value) : ConceptAs<string>(Value);

    SecurityMetadata _result;

    void Because() => _result = provider.Provide(typeof(PartnerWebhookSecret));

    [Fact] void should_resolve_to_the_namespace_metadata_type() => _result.MetadataType.ShouldEqual(SecurityMetadataType.EncryptedNamespace);
}
