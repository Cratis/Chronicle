// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Serverless.for_ServerlessConfigurationValidation.when_validating;

public class and_receiver_url_is_http_outside_development : Specification
{
    IReadOnlyList<string> _failures;

    void Because() => _failures = ServerlessConfigurationValidation.Validate(new ChronicleOptions { ProgramIdentifier = "orders", Serverless = { DeploymentGeneration = 1, ReceiverUrl = new Uri("http://orders.example.com") } }, "Production");

    [Fact] void should_fail_once() => _failures.Count.ShouldEqual(1);
    [Fact] void should_name_the_receiver_url() => _failures[0].Contains("ReceiverUrl").ShouldBeTrue();
    [Fact] void should_require_https() => _failures[0].Contains("https").ShouldBeTrue();
}
