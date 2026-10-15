// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Compliance.GDPR;

namespace Cratis.Chronicle.Schemas.for_JsonSchemaGenerator;

public class when_generating_an_event_type_with_a_defaulted_decimal_concept : given.a_json_schema_generator_with_pii_support
{
    JsonSchema _result;

    void Because() => _result = _generator.Generate(typeof(AmountRecorded));

    [Fact] void should_preserve_the_decimal_format() => _result.ActualProperties["amount"].Format.ShouldEqual("decimal?");
    [Fact] void should_preserve_the_null_default() => _result.ActualProperties["amount"].GetDefaultValue(_typeFormats).ShouldBeNull();
    [Fact] void should_preserve_the_pii_metadata() => _result.ActualProperties["amount"].GetComplianceMetadata().ShouldNotBeEmpty();

    [PII]
    record Amount(decimal Value) : ConceptAs<decimal>(Value);
    record AmountRecorded(Amount? Amount = null);
}
