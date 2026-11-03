// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Net;
using Cratis.Chronicle.Observation.Webhooks.Validation;

namespace Cratis.Chronicle.Captures.Engine.for_ApiCaptureSourceReader.when_reading.with_a_guarded_client;

public class and_the_service_resolves_to_loopback_in_warn_mode : given.a_reader_with_a_guarded_client
{
    void Establish()
    {
        _resolver.Resolve("capture.example", Arg.Any<CancellationToken>()).Returns([IPAddress.Loopback]);
        Configure(new() { Validation = WebhookValidationMode.Warn });
    }

    async Task Because() => await _reader.Read(_eventStore, _source);

    [Fact] void should_send_the_request() => _transport.Requests.Count.ShouldEqual(1);
    [Fact] void should_log_a_warning() => _warnings.ShouldNotBeEmpty();
}
