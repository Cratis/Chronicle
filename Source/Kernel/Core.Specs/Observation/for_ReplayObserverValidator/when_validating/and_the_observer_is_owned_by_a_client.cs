// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Observation;
using FluentValidation.Results;

namespace Cratis.Chronicle.Observation.for_ReplayObserverValidator.when_validating;

public class and_the_observer_is_owned_by_a_client : given.a_validator
{
    const string ObserverId = "some-projection";

    ValidationResult _result;

    void Establish() => ObserverIsOwnedBy(ObserverId, ObserverOwner.Client);

    async Task Because() => _result = await _validator.ValidateAsync(new ReplayObserver(EventStore, Namespace, ObserverId, string.Empty));

    [Fact] void should_be_valid() => _result.IsValid.ShouldBeTrue();
}
