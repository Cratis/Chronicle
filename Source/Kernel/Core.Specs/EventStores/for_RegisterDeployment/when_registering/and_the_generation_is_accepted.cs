// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Validation;

namespace Cratis.Chronicle.EventStores.for_RegisterDeployment.when_registering;

public class and_the_generation_is_accepted : given.a_registration
{
    ValidationResult? _validation;

    async Task Establish() => _validation = await new RegisterDeployment("the-store", 10UL, "fingerprint").Handle(_grainFactory);
    async Task Because() => _result = await _scenario.Execute(new RegisterDeployment("the-store", 10UL, "fingerprint"));

    [Fact] void should_return_no_validation_error() => _validation.ShouldBeNull();
    [Fact] void should_succeed_as_a_command() => _result.IsSuccess.ShouldBeTrue();
}
