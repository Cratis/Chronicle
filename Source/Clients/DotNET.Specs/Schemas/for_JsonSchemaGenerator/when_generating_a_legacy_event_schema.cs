// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Schemas.for_JsonSchemaGenerator;

public class when_generating_a_legacy_event_schema : given.a_json_schema_generator_with_pii_support
{
    record Amount(decimal Value) : ConceptAs<decimal>(Value);
    record AmountRecorded(Amount? Amount = null);
    JsonSchema _result;

    void Because() => _result = _generator.GenerateLegacyEventType(typeof(AmountRecorded));

    [Fact] void should_keep_the_default_only_shape() => _result.ActualProperties["amount"].ToJson().ShouldEqual("{\"default\":null}");
}
