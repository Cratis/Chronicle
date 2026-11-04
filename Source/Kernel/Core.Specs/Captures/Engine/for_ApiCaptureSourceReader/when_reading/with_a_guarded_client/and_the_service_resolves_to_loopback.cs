// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Net;
using Cratis.Chronicle.Observation.Webhooks.Validation;

namespace Cratis.Chronicle.Captures.Engine.for_ApiCaptureSourceReader.when_reading.with_a_guarded_client;

public class and_the_service_resolves_to_loopback : given.a_reader_with_a_guarded_client
{
    Exception _exception;

    void Establish()
    {
        _resolver.Resolve("capture.example", Arg.Any<CancellationToken>()).Returns([IPAddress.Loopback]);
        Configure(new() { Validation = WebhookValidationMode.BlockLocalAddresses });
    }

    async Task Because() => _exception = await Catch.Exception(() => _reader.Read(_eventStore, _source));

    [Fact] void should_refuse_the_destination() => _exception.ShouldBeOfExactType<WebhookTargetRejected>();
    [Fact] void should_send_nothing() => _transport.Requests.ShouldBeEmpty();
}
