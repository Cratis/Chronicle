// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Configuration;

namespace Cratis.Chronicle.Security.for_ExternalAdministratorClaimValidation;

public class when_type_and_value_are_set_with_an_external_authority : given.a_validation
{
    void Because() => _result = _validation.Validate(null, Options(new ExternalAdministratorClaim { Type = "roles", Value = "chronicle-admin" }));

    [Fact] void should_succeed() => _result.Succeeded.ShouldBeTrue();
}
