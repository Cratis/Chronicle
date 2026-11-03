// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Captures.Engine.for_CaptureRoute.when_resolving;

public class an_absolute_route_with_another_scheme : Specification
{
    Exception _exception;

    void Because() => _exception = Catch.Exception(() => CaptureRoute.Resolve("OrdersApi", new Uri("https://orders.example/api/"), "http://orders.example/orders"));

    [Fact] void should_refuse_the_route() => _exception.ShouldBeOfExactType<CaptureRouteOutsideExternalService>();
}
