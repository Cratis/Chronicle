// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Connections;

namespace Cratis.Chronicle.Serverless.for_ServerlessActivation.when_applying;

public class and_serverless_is_on_through_the_connection_string : Specification
{
    ChronicleOptions _options;

    void Establish() => _options = new ChronicleOptions
    {
        ProgramIdentifier = "orders",
        ConnectionString = new ChronicleConnectionString("chronicle://localhost:35000/?serverless=true"),
        Serverless = { Registration = ServerlessRegistration.OnColdStart }
    };

    void Because() => ServerlessActivation.Apply(_options, "Development");

    [Fact] void should_skip_keep_alive() => _options.SkipKeepAlive.ShouldBeTrue();
    [Fact] void should_register_inline() => _options.RegistrationMode.ShouldEqual(RegistrationMode.Inline);
}
