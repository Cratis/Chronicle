// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Captures.Engine.for_CaptureRoute.when_resolving;

public class a_root_relative_route : Specification
{
    Uri _result;

    void Because() => _result = CaptureRoute.Resolve("OrdersApi", new Uri("https://orders.example/api/"), "/orders");

    [Fact] void should_resolve_against_the_origin() => _result.ShouldEqual(new Uri("https://orders.example/orders"));
}
