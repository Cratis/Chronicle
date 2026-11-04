// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Captures.Engine.for_CaptureRoute.when_resolving;

public class a_route_to_a_host_with_an_encoded_dot : Specification
{
    Exception _exception;

    void Because() => _exception = Catch.Exception(() => CaptureRoute.Resolve("OrdersApi", new Uri("https://service.example.com/"), "https://evil%2Eexample.com/x"));

    [Fact] void should_reject_the_route_as_malformed() => _exception.ShouldBeOfExactType<UriFormatException>();
}
