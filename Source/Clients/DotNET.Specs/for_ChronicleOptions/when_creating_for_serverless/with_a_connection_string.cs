// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Serverless;

namespace Cratis.Chronicle.for_ChronicleOptions.when_creating_for_serverless;

public class with_a_connection_string : Specification
{
    ChronicleOptions _options;

    void Because() => _options = ChronicleOptions.ForServerless("chronicle://serverless:9090", "orders", new Uri("https://orders.example.com"));

    [Fact] void should_turn_serverless_on() => _options.Serverless.Mode.ShouldEqual(ServerlessMode.Serverless);
    [Fact] void should_set_the_program_identifier() => _options.ProgramIdentifier.ShouldEqual("orders");
    [Fact] void should_set_the_receiver_url() => _options.Serverless.ReceiverUrl.ShouldEqual(new Uri("https://orders.example.com"));
    [Fact] void should_skip_keep_alive() => _options.SkipKeepAlive.ShouldBeTrue();
    [Fact] void should_register_at_deployment() => _options.RegistrationMode.ShouldEqual(RegistrationMode.AtDeployment);
    [Fact] void should_parse_the_connection_string() => _options.ConnectionString.ServerAddress.Host.ShouldEqual("serverless");
}
