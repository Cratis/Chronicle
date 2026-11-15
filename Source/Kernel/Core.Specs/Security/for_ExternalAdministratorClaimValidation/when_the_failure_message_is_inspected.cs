// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Configuration;

namespace Cratis.Chronicle.Security.for_ExternalAdministratorClaimValidation;

public class when_the_failure_message_is_inspected : given.a_validation
{
    void Because() => _result = _validation.Validate(null, Options(new ExternalAdministratorClaim { Type = "roles", Value = "secret-admin-value " }));

    [Fact] void should_fail() => _result.Failed.ShouldBeTrue();
    [Fact] void should_not_contain_the_configured_value() => _result.FailureMessage.ShouldNotContain("secret-admin-value");
}
