// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Serverless.for_ServerlessConfigurationValidation.when_validating;

public class and_receiver_url_is_relative : Specification
{
    IReadOnlyList<string> _failures;

    void Because() => _failures = ServerlessConfigurationValidation.Validate(new ChronicleOptions { ProgramIdentifier = "orders", Serverless = { DeploymentGeneration = 1, ReceiverUrl = new Uri("/relative", UriKind.Relative) } }, "Production");

    [Fact] void should_fail_once() => _failures.Count.ShouldEqual(1);
    [Fact] void should_name_the_receiver_url() => _failures[0].Contains("ReceiverUrl").ShouldBeTrue();
}
