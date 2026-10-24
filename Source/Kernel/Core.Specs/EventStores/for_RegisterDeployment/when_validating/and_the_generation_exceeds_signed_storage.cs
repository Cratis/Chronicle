// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.EventStores.for_RegisterDeployment.when_validating;

public class and_the_generation_exceeds_signed_storage : given.a_registration
{
    async Task Because() => _result = await _scenario.Validate(new RegisterDeployment("the-store", 9_223_372_036_854_775_808UL, "fingerprint"));

    [Fact] void should_reject() => _result.IsSuccess.ShouldBeFalse();
    [Fact] void should_report_a_validation_error() => _result.ValidationResults.ShouldNotBeEmpty();
}
