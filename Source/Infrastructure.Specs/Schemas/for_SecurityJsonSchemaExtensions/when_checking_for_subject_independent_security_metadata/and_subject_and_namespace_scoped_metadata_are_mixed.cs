// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Schemas.for_SecurityJsonSchemaExtensions.when_checking_for_subject_independent_security_metadata;

public class and_subject_and_namespace_scoped_metadata_are_mixed : Specification
{
    const string Json = """
    {
        "type": "object",
        "properties": {
            "apiKey": { "type": "string", "security": [ { "metadataType": "EncryptedSubject", "details": "" } ] },
            "webhookSecret": { "type": "string", "security": [ { "metadataType": "EncryptedNamespace", "details": "" } ] }
        }
    }
    """;

    JsonSchema _schema;
    bool _result;

    void Establish() => _schema = JsonSchema.FromJson(Json);

    void Because() => _result = _schema.HasSubjectIndependentSecurityMetadata();

    [Fact] void should_recognize_it_as_subject_independent() => _result.ShouldBeTrue();
}
