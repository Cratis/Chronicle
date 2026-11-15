// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Serverless.for_ServerlessConfigurationValidation.when_validating;

public class and_auth_type_is_undefined : Specification
{
    IReadOnlyList<string> _failures;

    void Because() => _failures = ServerlessConfigurationValidation.Validate(new ChronicleOptions { ProgramIdentifier = "orders", Serverless = { DeploymentGeneration = 1, Auth = { Type = (ReceiverAuthenticationType)42 } } }, "Production");

    [Fact] void should_fail_once() => _failures.Count.ShouldEqual(1);
    [Fact] void should_name_the_setting() => _failures[0].Contains("Auth.Type").ShouldBeTrue();
    [Fact] void should_report_the_undefined_value() => _failures[0].Contains("undefined value 42").ShouldBeTrue();
}
