// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Captures.Engine.for_CaptureRoute.when_resolving;

public class a_route_with_the_service_name_as_user_information : Specification
{
    Exception _exception;

    void Because() => _exception = Catch.Exception(() => CaptureRoute.Resolve("OrdersApi", new Uri("https://service.example.com/"), "https://service.example.com@evil.example.com/x"));

    [Fact] void should_refuse_the_route() => _exception.ShouldBeOfExactType<CaptureRouteOutsideExternalService>();
}
