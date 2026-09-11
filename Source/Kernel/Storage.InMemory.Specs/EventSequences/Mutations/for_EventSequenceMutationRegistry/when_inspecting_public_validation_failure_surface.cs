// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;
using Cratis.Chronicle.Storage.EventSequences.Mutations;

namespace Cratis.Chronicle.Storage.InMemory.EventSequences.Mutations.for_EventSequenceMutationRegistry;

public class when_inspecting_public_validation_failure_surface : Specification
{
    EventSequenceMutationValidationResult _identityValidation;
    EventSequenceMutationValidationResult _trackingValidation;
    MethodInfo[] _publicValidationFactories;
    ConstructorInfo[] _publicValidationConstructors;
    MethodInfo[] _publicValidatorMethods;

    void Because()
    {
        _identityValidation = EventSequenceMutationValidator.ValidateIdentity(null);
        _trackingValidation = EventSequenceMutationValidator.ValidateTrackingCoverage(EventSequenceMutationCoverage.Sealed);
        _publicValidationFactories = typeof(EventSequenceMutationValidationResult).GetMethods(BindingFlags.Public | BindingFlags.Static);
        _publicValidationConstructors = typeof(EventSequenceMutationValidationResult).GetConstructors();
        _publicValidatorMethods = typeof(EventSequenceMutationValidator)
            .GetMethods(BindingFlags.Public | BindingFlags.Static)
            .Where(_ => _.ReturnType == typeof(EventSequenceMutationValidationResult))
            .ToArray();
    }

    [Fact] void should_not_expose_a_public_validation_result_constructor() => _publicValidationConstructors.ShouldBeEmpty();
    [Fact] void should_not_expose_a_public_failed_validation_factory() => _publicValidationFactories.Any(_ => _.Name == "Failed").ShouldBeFalse();
    [Fact] void should_not_accept_a_free_form_field_through_any_public_validator() => _publicValidatorMethods.SelectMany(_ => _.GetParameters()).Any(_ => _.ParameterType == typeof(string)).ShouldBeFalse();
    [Fact] void should_use_the_identity_parameter_name_as_the_identity_failure_field() => _identityValidation.Field.ShouldEqual(ParameterName(nameof(EventSequenceMutationValidator.ValidateIdentity)));
    [Fact] void should_use_the_expected_parameter_name_as_the_tracking_failure_field() => _trackingValidation.Field.ShouldEqual(ParameterName(nameof(EventSequenceMutationValidator.ValidateTrackingCoverage)));

    static string ParameterName(string methodName) =>
        typeof(EventSequenceMutationValidator)
            .GetMethods(BindingFlags.Public | BindingFlags.Static)
            .Single(_ => _.Name == methodName)
            .GetParameters()
            .Single()
            .Name!;
}
