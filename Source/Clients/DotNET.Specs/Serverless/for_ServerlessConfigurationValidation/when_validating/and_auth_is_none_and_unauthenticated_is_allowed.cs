// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Serverless.for_ServerlessConfigurationValidation.when_validating;

public class and_auth_is_none_and_unauthenticated_is_allowed : Specification
{
    IReadOnlyList<string> _failures;

    void Because() => _failures = ServerlessConfigurationValidation.Validate(new ChronicleOptions { ProgramIdentifier = "orders", Serverless = { DeploymentGeneration = 1, Auth = { Type = ReceiverAuthenticationType.None, AllowUnauthenticated = true } } }, "Production");

    [Fact] void should_not_fail() => _failures.ShouldBeEmpty();
}
