// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Schemas.for_ComplianceJsonSchemaExtensions.when_checking_for_compliance_metadata;

/// <summary>
/// The first call runs the uncached recursive walk; the answer is memoized on the instance. Repeated calls must
/// return exactly that first answer, for both a schema that carries compliance metadata and one that does not.
/// </summary>
public class and_the_result_is_memoized : Specification
{
    const string WithMetadata = """
    {
        "type": "object",
        "properties": {
            "socialSecurityNumber": { "type": "string", "compliance": [ { "metadataType": "PII", "details": "" } ] }
        }
    }
    """;

    const string WithoutMetadata = """
    {
        "type": "object",
        "properties": { "name": { "type": "string" } }
    }
    """;

    JsonSchema _withMetadata;
    JsonSchema _withoutMetadata;
    bool[] _withMetadataResults;
    bool[] _withoutMetadataResults;

    void Establish()
    {
        _withMetadata = JsonSchema.FromJson(WithMetadata);
        _withoutMetadata = JsonSchema.FromJson(WithoutMetadata);
    }

    void Because()
    {
        _withMetadataResults = Enumerable.Range(0, 50).Select(_ => _withMetadata.HasComplianceMetadata()).ToArray();
        _withoutMetadataResults = Enumerable.Range(0, 50).Select(_ => _withoutMetadata.HasComplianceMetadata()).ToArray();
    }

    [Fact] void should_return_true_on_every_call_when_metadata_is_present() => _withMetadataResults.Distinct().ShouldContainOnly([true]);
    [Fact] void should_return_false_on_every_call_when_metadata_is_absent() => _withoutMetadataResults.Distinct().ShouldContainOnly([false]);
}
