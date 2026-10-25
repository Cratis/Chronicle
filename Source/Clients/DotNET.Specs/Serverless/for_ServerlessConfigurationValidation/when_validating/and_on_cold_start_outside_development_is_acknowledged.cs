// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Serverless.for_ServerlessConfigurationValidation.when_validating;

public class and_on_cold_start_outside_development_is_acknowledged : Specification
{
    IReadOnlyList<string> _failures;

    void Because() => _failures = ServerlessConfigurationValidation.Validate(new ChronicleOptions { ProgramIdentifier = "orders", Serverless = { Registration = ServerlessRegistration.OnColdStart, AllowOnColdStartOutsideDevelopment = true } }, "Production");

    [Fact] void should_not_fail() => _failures.ShouldBeEmpty();
}
