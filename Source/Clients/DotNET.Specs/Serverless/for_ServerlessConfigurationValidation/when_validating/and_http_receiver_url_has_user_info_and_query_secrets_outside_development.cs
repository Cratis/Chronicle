// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Serverless.for_ServerlessConfigurationValidation.when_validating;

public class and_http_receiver_url_has_user_info_and_query_secrets_outside_development : Specification
{
    IReadOnlyList<string> _failures;
    string _message;

    void Because()
    {
        _failures = ServerlessConfigurationValidation.Validate(new ChronicleOptions { ProgramIdentifier = "orders", Serverless = { DeploymentGeneration = 1, ReceiverUrl = new Uri("http://user:SECRET@example.com/path?code=SECRET#SECRET") } }, "Production");
        _message = ServerlessConfigurationValidation.Format(_failures);
    }

    [Fact] void should_fail_once() => _failures.Count.ShouldEqual(1);
    [Fact] void should_not_leak_the_secret_in_the_failure() => _failures[0].Contains("SECRET").ShouldBeFalse();
    [Fact] void should_not_leak_the_secret_in_the_message() => _message.Contains("SECRET").ShouldBeFalse();
}
