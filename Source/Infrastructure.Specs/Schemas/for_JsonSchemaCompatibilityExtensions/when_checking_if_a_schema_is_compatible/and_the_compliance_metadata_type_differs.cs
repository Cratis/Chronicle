// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Schemas.for_JsonSchemaCompatibilityExtensions.when_checking_if_a_schema_is_compatible;

/// <summary>
/// Regression for https://github.com/Cratis/Chronicle/issues/4660 — the details text is documentation, not part of
/// the stored schema's meaning; the metadata type and scope are.
/// </summary>
public class and_the_compliance_metadata_type_differs : Specification
{
    const string Stored = """{"type":"object","properties":{"amount":{"type":"string","compliance":[{"metadataType":"PII","details":"Same"}]}}}""";
    const string Generated = """{"type":"object","properties":{"amount":{"type":"string","compliance":[{"metadataType":"GDPR","details":"Same"}]}}}""";

    bool _result;

    void Because() => _result = JsonSchema.FromJson(Stored).IsCompatibleWith(JsonSchema.FromJson(Generated));

    [Fact] void should_consider_them_incompatible() => _result.ShouldBeFalse();
}
