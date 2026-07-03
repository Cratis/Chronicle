// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.ExternalServices;
using Cratis.Chronicle.Services.ExternalServices;

namespace Cratis.Chronicle.ExternalServices.for_ExternalServiceContractConverters;

public class when_round_tripping_an_http_service_with_bearer_auth : Specification
{
    ExternalServiceDefinition _original;
    ExternalServiceDefinition _result;

    void Establish() => _original = new ExternalServiceDefinition(
        "CustomersApi",
        "Customers API",
        new ExternalServiceEndpoint(
            ExternalServiceEndpointType.Http,
            Http: new HttpEndpointConfiguration(
                "https://api.example.com",
                new Concepts.Security.BearerTokenAuthorization("the-token"),
                new Dictionary<string, string> { { "X-Tenant", "acme" } })));

    void Because() => _result = _original.ToContract().ToKernel();

    [Fact] void should_preserve_id() => _result.Id.ShouldEqual(_original.Id);
    [Fact] void should_preserve_name() => _result.Name.ShouldEqual(_original.Name);
    [Fact] void should_preserve_endpoint_type() => _result.Endpoint.Type.ShouldEqual(ExternalServiceEndpointType.Http);
    [Fact] void should_preserve_url() => _result.Endpoint.Http!.Url.ShouldEqual(_original.Endpoint.Http!.Url);
    [Fact] void should_preserve_bearer_token() => _result.Endpoint.Http!.Authorization.AsT1.Token.Value.ShouldEqual("the-token");
    [Fact] void should_preserve_headers() => _result.Endpoint.Http!.Headers["X-Tenant"].ShouldEqual("acme");
    [Fact] void should_have_no_database_configuration() => _result.Endpoint.Database.ShouldBeNull();
}
