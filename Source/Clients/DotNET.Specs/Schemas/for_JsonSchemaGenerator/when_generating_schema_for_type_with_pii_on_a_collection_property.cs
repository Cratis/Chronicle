// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Compliance;
using Cratis.Chronicle.Compliance.GDPR;

namespace Cratis.Chronicle.Schemas.for_JsonSchemaGenerator;

/// <summary>
/// A coarse marker on a whole collection stays on the collection: it is blob-encrypted as one value and its
/// shape restored on release. Descending into the items instead would change that established behavior.
/// </summary>
public class when_generating_schema_for_type_with_pii_on_a_collection_property : given.a_json_schema_generator_with_pii_support
{
    record PersonAliases(string Id, [property: PII] IReadOnlyList<string> Aliases);

    JsonSchema _result;

    void Because() => _result = _generator.Generate(typeof(PersonAliases));

    [Fact] void should_keep_compliance_metadata_on_the_collection_itself() =>
        _result.ActualProperties["aliases"].GetComplianceMetadata()
            .Select(_ => _.metadataType).ShouldContain(ComplianceMetadataType.PII.Value);
}
