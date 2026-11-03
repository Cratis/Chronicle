// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Observation.Webhooks.Validation;

namespace Cratis.Chronicle.Captures.Engine.for_ApiCaptureSourceReader.when_reading.with_a_guarded_client;

public class and_the_route_points_to_another_host : given.a_reader_with_a_guarded_client
{
    Exception _exception;

    void Establish()
    {
        _source = _source with { Route = "https://other.example/items" };
        Configure(new() { Validation = WebhookValidationMode.BlockLocalAddresses });
    }

    async Task Because() => _exception = await Catch.Exception(() => _reader.Read(_eventStore, _source));

    [Fact] void should_refuse_the_route() => _exception.ShouldBeOfExactType<CaptureRouteOutsideExternalService>();
    [Fact] void should_send_nothing() => _transport.Requests.ShouldBeEmpty();
    [Fact] void should_not_resolve_the_other_host() => _resolver.DidNotReceive().Resolve("other.example", Arg.Any<CancellationToken>());
}
