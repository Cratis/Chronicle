// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.EventStores.for_RegisterDeployment.when_validating;

public class and_the_fingerprint_is_too_long : given.a_registration
{
    async Task Because() => _result = await _scenario.Validate(new RegisterDeployment("the-store", 10UL, new string('a', 257)));

    [Fact] void should_reject() => _result.IsSuccess.ShouldBeFalse();
    [Fact] void should_report_a_validation_error() => _result.ValidationResults.ShouldNotBeEmpty();
}
