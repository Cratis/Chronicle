// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.ExternalServices;
using Cratis.Chronicle.Helpers;
using Cratis.Chronicle.Observation.Webhooks;

namespace Cratis.Chronicle.ExternalServices.for_ExternalServiceHttpClientFactory;

public class when_creating_with_invalid_headers : Helpers.for_CustomHttpHeaders.given.invalid_headers
{
    readonly HttpClient _client = new();
    ExternalServiceHttpClientFactory _factory;
    Exception?[] _errors;

    void Establish()
    {
        var transport = Substitute.For<IHttpClientFactory>();
        transport.CreateClient(Arg.Any<string>()).Returns(_client);
        _factory = new(transport, Substitute.For<IOAuthClient>());
    }

    async Task Because() => _errors = await Task.WhenAll(_invalidHeaders.Select(headers =>
        Catch.Exception(async () => await _factory.Create(new ExternalServiceDefinition("service", "service", new(ExternalServiceEndpointType.Http,
            new("https://receiver.example", ExternalServiceAuthorization.None, headers)))))));

    [Fact] void should_reject_every_invalid_header() => _errors.All(error => error is InvalidCustomHttpHeader).ShouldBeTrue();
    [Fact] void should_not_include_the_value_in_exception_text() => _errors.All(error => error?.ToString().Contains(Secret) == false).ShouldBeTrue();
    [Fact] void should_not_retain_the_parser_exception() => _errors.All(error => error is { InnerException: null }).ShouldBeTrue();

    void Destroy() => _client.Dispose();
}
