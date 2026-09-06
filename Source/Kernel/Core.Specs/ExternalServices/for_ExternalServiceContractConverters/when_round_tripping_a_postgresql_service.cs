// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.ExternalServices;

namespace Cratis.Chronicle.ExternalServices.for_ExternalServiceContractConverters;

public class when_round_tripping_a_postgresql_service : Specification
{
    ExternalServiceDefinition _original;
    ExternalServiceDefinition _result;

    void Establish() => _original = new ExternalServiceDefinition(
        "CustomersDb",
        "Customers Database",
        new ExternalServiceEndpoint(
            ExternalServiceEndpointType.PostgreSql,
            Database: new DatabaseEndpointConfiguration(
                "db.example.com",
                5432,
                "customers",
                "postgres",
                "secret",
                new Dictionary<string, string> { { "SslMode", "Require" } })));

    void Because() => _result = _original.ToContract().ToKernel();

    [Fact] void should_preserve_endpoint_type() => _result.Endpoint.Type.ShouldEqual(ExternalServiceEndpointType.PostgreSql);
    [Fact] void should_preserve_host() => _result.Endpoint.Database!.Host.Value.ShouldEqual("db.example.com");
    [Fact] void should_preserve_port() => _result.Endpoint.Database!.Port.Value.ShouldEqual(5432);
    [Fact] void should_preserve_database() => _result.Endpoint.Database!.Database.Value.ShouldEqual("customers");
    [Fact] void should_preserve_username() => _result.Endpoint.Database!.Username.Value.ShouldEqual("postgres");
    [Fact] void should_preserve_password() => _result.Endpoint.Database!.Password.Value.ShouldEqual("secret");
    [Fact] void should_preserve_options() => _result.Endpoint.Database!.Options["SslMode"].ShouldEqual("Require");
    [Fact] void should_have_no_http_configuration() => _result.Endpoint.Http.ShouldBeNull();
}
