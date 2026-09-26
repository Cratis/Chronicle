// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Observation;
using FluentValidation.Results;

namespace Cratis.Chronicle.Observation.for_ReplayObserverValidator.when_validating;

public class and_the_observer_is_owned_by_the_kernel : given.a_validator
{
    const string ObserverId = "$system.event-type-distribution";

    ValidationResult _result;

    void Establish() => ObserverIsOwnedBy(ObserverId, ObserverOwner.Kernel);

    async Task Because() => _result = await _validator.ValidateAsync(new ReplayObserver(EventStore, Namespace, ObserverId, string.Empty));

    [Fact] void should_not_be_valid() => _result.IsValid.ShouldBeFalse();

    [Fact] void should_say_the_kernel_owns_it() =>
        _result.Errors.Exists(_ => _.ErrorMessage == "Observers owned by the kernel cannot be replayed.").ShouldBeTrue();
}
