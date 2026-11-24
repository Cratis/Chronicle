// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Observation.Webhooks;
using Cratis.Chronicle.Helpers;

namespace Cratis.Chronicle.Observation.Webhooks.for_WebhookHttpClientFactory.when_creating;

public class with_invalid_headers : Helpers.for_CustomHttpHeaders.given.invalid_headers
{
    readonly HttpClient _client = new();
    WebhookHttpClientFactory _factory;
    Exception?[] _errors;

    void Establish()
    {
        var transport = Substitute.For<IHttpClientFactory>();
        transport.CreateClient(Arg.Any<string>()).Returns(_client);
        _factory = new(transport, Substitute.For<IOAuthClient>());
    }

    void Because() => _errors = _invalidHeaders.Select(headers => Catch.Exception(() =>
        _factory.Create(new("https://receiver.example", WebhookAuthorization.None, headers)))).ToArray();

    [Fact] void should_reject_every_invalid_header() => _errors.All(error => error is InvalidCustomHttpHeader).ShouldBeTrue();
    [Fact] void should_not_include_the_value_in_exception_text() => _errors.All(error => error?.ToString().Contains(Secret) == false).ShouldBeTrue();
    [Fact] void should_not_retain_the_parser_exception() => _errors.All(error => error is { InnerException: null }).ShouldBeTrue();

    void Destroy() => _client.Dispose();
}
