// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Compliance.GDPR;
using Cratis.Chronicle.Events;

namespace Cratis.Chronicle.Schemas.for_JsonSchemaGenerator;

/// <summary>
/// Event type schemas keep the shape already registered with the kernel, which refuses a schema change within a generation.
/// </summary>
public class when_generating_event_type_schema_for_record_with_nullable_concept_parameters_defaulting_to_null : given.a_json_schema_generator_with_pii_support
{
    [PII]
    record OwnerSubject(string Value) : ConceptAs<string>(Value)
    {
        public static implicit operator OwnerSubject(string value) => new(value);
    }

    record OrganizationSetupStarted(string Name, OwnerSubject? OwnerSubject = null, EventSequenceNumber? LastHandled = null);

    JsonSchema _result;

    void Because() => _result = _generator.Generate(typeof(OrganizationSetupStarted));

    [Fact] void should_keep_the_default_only_schema_for_the_string_concept() => _result.ActualProperties["ownerSubject"].ToJson().ShouldEqual("{\"default\":null}");
    [Fact] void should_keep_the_default_only_schema_for_the_numeric_concept() => _result.ActualProperties["lastHandled"].ToJson().ShouldEqual("{\"default\":null}");
    [Fact] void should_leave_the_other_properties_alone() => _result.ActualProperties["name"].ToJson().ShouldEqual("{\"type\":\"string\"}");
}
