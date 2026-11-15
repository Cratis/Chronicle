// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Serverless.for_ServerlessConfigurationValidation.when_validating;

public class and_several_rules_fail : Specification
{
    IReadOnlyList<string> _failures;

    void Because() => _failures = ServerlessConfigurationValidation.Validate(new ChronicleOptions { Serverless = { Auth = { Type = ReceiverAuthenticationType.None } } }, "Production");

    [Fact] void should_list_every_failure() => _failures.Count.ShouldEqual(3);
}
