// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Serverless.for_ServerlessConfigurationValidation.when_validating;

public class and_receiver_url_is_http_in_development : Specification
{
    IReadOnlyList<string> _failures;

    void Because() => _failures = ServerlessConfigurationValidation.Validate(new ChronicleOptions { ProgramIdentifier = "orders", Serverless = { ReceiverUrl = new Uri("http://localhost:7071") } }, "Development");

    [Fact] void should_not_fail() => _failures.Count.ShouldEqual(0);
}
