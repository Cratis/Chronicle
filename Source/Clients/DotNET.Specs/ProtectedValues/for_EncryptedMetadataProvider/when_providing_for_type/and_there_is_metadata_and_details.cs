// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Compliance;

namespace Cratis.Chronicle.ProtectedValues.for_EncryptedMetadataProvider.when_providing_for_type;

public class and_there_is_metadata_and_details : given.a_provider
{
    const string Details = "These are the details";

    [Encrypted]
    [ComplianceDetails(Details)]
    record MyConceptValue(string Value) : ConceptAs<string>(Value);

    ComplianceMetadata _result;

    void Because() => _result = provider.Provide(typeof(MyConceptValue));

    [Fact] void should_return_encrypted_subject_metadata() => _result.MetadataType.ShouldEqual(ComplianceMetadataType.EncryptedSubject);
    [Fact] void should_return_metadata_with_details() => _result.Details.ShouldEqual(Details);
}
