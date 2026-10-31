// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Observation.Webhooks.Validation;

namespace Cratis.Chronicle.Captures.Engine.for_ApiCaptureSourceReader.when_reading;

public class and_the_request_carries_the_event_store : given.an_api_capture_source_reader
{
    async Task Because() => await _reader.Read(_eventStore, _source);

    [Fact]
    void should_carry_the_event_store()
    {
        _request.Options.TryGetValue(WebhookRequestOptions.EventStore, out var eventStore).ShouldBeTrue();
        eventStore.ShouldEqual(_eventStore);
    }
    [Fact] void should_validate_the_event_store() => _policy.Received().CheckAuthority(new Uri("https://orders.example/orders"), _eventStore);
}
