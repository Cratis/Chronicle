// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Observation.Webhooks.Validation;

namespace Cratis.Chronicle.Captures.Engine.for_ApiCaptureSourceReader.when_reading.with_a_guarded_client;

public class and_the_route_is_relative : given.a_reader_with_a_guarded_client
{
    void Establish() => Configure(new() { Validation = WebhookValidationMode.BlockLocalAddresses });

    async Task Because() => await _reader.Read(_eventStore, _source);

    [Fact] void should_send_once() => _transport.Requests.Count.ShouldEqual(1);
    [Fact] void should_resolve_against_the_service() => _transport.Requests[0].Target.ShouldEqual(new Uri("https://capture.example/items"));
    [Fact] void should_apply_the_authorization() => _transport.Requests[0].Authorization.ShouldEqual("Bearer token");
    [Fact] void should_apply_the_custom_header() => _transport.Requests[0].HasApiKey.ShouldBeTrue();
}
