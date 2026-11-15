// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Security.for_ExternalAdministratorClaimValidation;

public class when_no_mapping_is_configured : given.a_validation
{
    void Because() => _result = _validation.Validate(null, Options(null));

    [Fact] void should_succeed() => _result.Succeeded.ShouldBeTrue();
}
