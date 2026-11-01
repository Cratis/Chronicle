// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Observation.TypeSets.for_RetireEventTypeFromObserver.when_validating;

public class and_the_event_type_is_missing : Specification
{
    FluentValidation.Results.ValidationResult _result;

    void Because() => _result = new RetireEventTypeFromObserverValidator().Validate(new RetireEventTypeFromObserver("store", null, "observer", "event-log", string.Empty));

    [Fact] void should_be_invalid() => _result.IsValid.ShouldBeFalse();
    [Fact] void should_identify_the_missing_event_type() => _result.Errors.Single().PropertyName.ShouldEqual(nameof(RetireEventTypeFromObserver.EventTypeId));
}
