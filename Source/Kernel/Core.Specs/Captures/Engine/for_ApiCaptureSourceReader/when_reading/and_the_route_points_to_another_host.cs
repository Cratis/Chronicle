// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;

namespace Cratis.Chronicle.Captures.Engine.for_ApiCaptureSourceReader.when_reading;

public class and_the_route_points_to_another_host : given.an_api_capture_source_reader
{
    Exception _exception;

    void Establish() => _source = _source with { Route = "https://other.example/orders" };

    async Task Because() => _exception = await Catch.Exception(() => _reader.Read(_eventStore, _source));

    [Fact] void should_refuse_the_route() => _exception.ShouldBeOfExactType<CaptureRouteOutsideExternalService>();
    [Fact] void should_not_send_the_request() => _request.ShouldBeNull();
    [Fact] void should_not_consult_the_destination_policy() => _policy.DidNotReceive().CheckAuthority(Arg.Any<Uri>(), Arg.Any<EventStoreName?>());
}
