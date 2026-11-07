// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Serverless.for_ServerlessConfigurationValidation.when_validating;

public class and_deploy_has_no_generation_outside_development : Specification
{
    IReadOnlyList<string> _failures;

    void Because() => _failures = ServerlessConfigurationValidation.Validate(new ChronicleOptions { ProgramIdentifier = "orders" }, "Production");

    [Fact] void should_fail_once() => _failures.Count.ShouldEqual(1);
    [Fact] void should_name_the_generation() => _failures[0].Contains("DeploymentGeneration").ShouldBeTrue();
}
