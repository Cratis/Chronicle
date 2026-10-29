// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Captures.Engine.for_ApiCaptureSourceReader.when_reading;

public class and_the_route_is_absolute : given.an_api_capture_source_reader
{
    void Establish() => _source = _source with { Route = "https://other.example/orders" };

    async Task Because() => await _reader.Read(_eventStore, _source);

    [Fact] void should_use_the_absolute_route() => _request.RequestUri.ShouldEqual(new Uri("https://other.example/orders"));
    [Fact] void should_validate_the_absolute_route() => _policy.Received().CheckAuthority(new Uri("https://other.example/orders"), _eventStore);
}
