// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Captures.Engine.for_CaptureRoute.when_resolving;

public class a_slash_backslash_route_to_another_host : Specification
{
    Uri _result;

    void Because() => _result = CaptureRoute.Resolve("OrdersApi", new Uri("https://service.example.com/"), @"/\evil.example.com");

    [Fact] void should_stay_on_the_origin() => _result.GetLeftPart(UriPartial.Authority).ShouldEqual("https://service.example.com");
}
