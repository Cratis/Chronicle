// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Captures.Engine.for_CaptureRoute.when_resolving;

public class an_absolute_route_on_the_same_origin_with_an_explicit_default_port : Specification
{
    Uri _result;

    void Because() => _result = CaptureRoute.Resolve("OrdersApi", new Uri("https://orders.example/api/"), "https://orders.example:443/x");

    [Fact] void should_accept_the_default_port() => _result.ShouldEqual(new Uri("https://orders.example/x"));
}
