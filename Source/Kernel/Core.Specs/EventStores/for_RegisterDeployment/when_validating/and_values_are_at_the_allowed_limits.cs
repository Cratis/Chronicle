// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.EventStores.for_RegisterDeployment.when_validating;

public class and_values_are_at_the_allowed_limits : given.a_registration
{
    async Task Because() => _result = await _scenario.Validate(new RegisterDeployment("the-store", 9_223_372_036_854_775_807UL, new string('a', 256)));

    [Fact] void should_accept() => _result.IsSuccess.ShouldBeTrue();
}
