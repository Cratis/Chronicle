// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Schemas.for_IJsonSchemaGenerator;

public class when_a_legacy_generator_cannot_generate_a_legacy_event_schema : Specification
{
    Exception _exception;

    void Because() => _exception = Catch.Exception(() => ((IJsonSchemaGenerator)new LegacyGenerator()).GenerateLegacyEventType(typeof(string)));

    [Fact] void should_fail_with_the_named_refusal() => _exception.ShouldBeOfExactType<LegacyEventTypeSchemasNotSupported>();

    class LegacyGenerator : IJsonSchemaGenerator
    {
        public JsonSchema Generate(Type type) => new();
    }
}
