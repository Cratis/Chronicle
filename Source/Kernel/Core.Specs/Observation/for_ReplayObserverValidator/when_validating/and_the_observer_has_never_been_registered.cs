// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Observation;
using FluentValidation.Results;

namespace Cratis.Chronicle.Observation.for_ReplayObserverValidator.when_validating;

public class and_the_observer_has_never_been_registered : given.a_validator
{
    ValidationResult _result;

    void Establish() => _observerDefinitions.Has(Arg.Any<ObserverId>()).Returns(false);

    async Task Because() => _result = await _validator.ValidateAsync(new ReplayObserver(EventStore, Namespace, "never-registered", string.Empty));

    [Fact] void should_leave_reporting_that_to_the_replay_itself() => _result.IsValid.ShouldBeTrue();
}
