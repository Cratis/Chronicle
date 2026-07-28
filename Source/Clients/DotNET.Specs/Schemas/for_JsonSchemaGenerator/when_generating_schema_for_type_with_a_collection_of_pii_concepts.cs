// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Compliance;
using Cratis.Chronicle.Compliance.GDPR;

namespace Cratis.Chronicle.Schemas.for_JsonSchemaGenerator;

public class when_generating_schema_for_type_with_a_collection_of_pii_concepts : given.a_json_schema_generator_with_pii_support
{
    [PII]
    record Alias(string Value) : ConceptAs<string>(Value);

    record PersonAliases(string Id, IReadOnlyList<Alias> Aliases);

    JsonSchema _result;

    void Because() => _result = _generator.Generate(typeof(PersonAliases));

    [Fact] void should_have_compliance_metadata() => _result.HasComplianceMetadata().ShouldBeTrue();

    [Fact] void should_carry_the_element_concepts_compliance_metadata_on_the_items() =>
        _result.ActualProperties["aliases"].Item!.GetComplianceMetadata()
            .Select(_ => _.metadataType).ShouldContain(ComplianceMetadataType.PII.Value);
}
