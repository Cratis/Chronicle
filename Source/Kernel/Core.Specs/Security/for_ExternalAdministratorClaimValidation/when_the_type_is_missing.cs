// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Configuration;

namespace Cratis.Chronicle.Security.for_ExternalAdministratorClaimValidation;

public class when_the_type_is_missing : given.a_validation
{
    void Because() => _result = _validation.Validate(null, Options(new ExternalAdministratorClaim { Value = "chronicle-admin" }));

    [Fact] void should_fail() => _result.Failed.ShouldBeTrue();
}
