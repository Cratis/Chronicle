// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Compliance;
using Cratis.Chronicle.Compliance.GDPR;
using Cratis.Chronicle.Events;

namespace Cratis.Chronicle.Schemas.for_JsonSchemaGenerator;

public class when_generating_read_model_schema_for_record_with_nullable_concept_parameters_defaulting_to_null : given.a_json_schema_generator_with_pii_support
{
    [PII]
    record OwnerSubject(string Value) : ConceptAs<string>(Value)
    {
        public static implicit operator OwnerSubject(string value) => new(value);
    }

    record OrganizationSetupProgress(
        string Name,
        OwnerSubject? OwnerSubjectWithoutDefault,
        OwnerSubject? OwnerSubject = null,
        EventSequenceNumber? LastHandled = null);

    JsonSchema _result;
    JsonSchemaProperty _ownerSubject;
    JsonSchemaProperty _ownerSubjectWithoutDefault;
    JsonSchemaProperty _lastHandled;

    void Because()
    {
        _result = _generator.GenerateForReadModel(typeof(OrganizationSetupProgress));
        _ownerSubject = _result.ActualProperties["ownerSubject"];
        _ownerSubjectWithoutDefault = _result.ActualProperties["ownerSubjectWithoutDefault"];
        _lastHandled = _result.ActualProperties["lastHandled"];
    }

    [Fact] void should_give_the_concept_its_underlying_type() => _ownerSubject.Type.HasFlag(JsonObjectType.String).ShouldBeTrue();
    [Fact] void should_give_the_concept_the_same_type_as_without_a_default() => _ownerSubject.Type.ShouldEqual(_ownerSubjectWithoutDefault.Type);
    [Fact] void should_carry_the_concepts_pii_compliance_metadata() => _ownerSubject.GetComplianceMetadata().Select(_ => _.metadataType).ShouldContain(ComplianceMetadataType.PII.Value);
    [Fact] void should_give_a_numeric_concept_its_nullable_format() => _lastHandled.Format.ShouldEqual("uint64?");
    [Fact] void should_give_a_numeric_concept_its_underlying_type() => _lastHandled.Type.HasFlag(JsonObjectType.Integer).ShouldBeTrue();
}
