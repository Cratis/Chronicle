// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Observation.TypeSets.for_RetireEventTypeFromObserver.when_validating;

public class and_all_values_are_provided : Specification
{
    FluentValidation.Results.ValidationResult _result;

    void Because() => _result = new RetireEventTypeFromObserverValidator().Validate(new RetireEventTypeFromObserver("store", "namespace", "observer", "event-log", "event-type"));

    [Fact] void should_be_valid() => _result.IsValid.ShouldBeTrue();
}
