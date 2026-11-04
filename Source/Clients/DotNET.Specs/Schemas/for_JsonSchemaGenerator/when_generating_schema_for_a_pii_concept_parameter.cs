// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Compliance;
using Cratis.Chronicle.Compliance.GDPR;

namespace Cratis.Chronicle.Schemas.for_JsonSchemaGenerator;

public class when_generating_schema_for_a_pii_concept_parameter : given.a_json_schema_generator_with_pii_support
{
    record Note(string Value) : ConceptAs<string>(Value);
    record Notes([PII("Parameter details")] Note Note);

    JsonSchema _result;

    void Because() => _result = _generator.Generate(typeof(Notes));

    [Fact] void should_include_parameter_metadata_on_the_concept() => _result.ActualProperties["note"].GetComplianceMetadata().Single().ShouldEqual(new ComplianceSchemaMetadata(ComplianceMetadataType.PII.Value, "Parameter details"));
}
