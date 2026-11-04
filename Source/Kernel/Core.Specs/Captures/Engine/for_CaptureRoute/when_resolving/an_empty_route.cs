// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Captures.Engine.for_CaptureRoute.when_resolving;

public class an_empty_route : Specification
{
    Uri _result;

    void Because() => _result = CaptureRoute.Resolve("OrdersApi", new Uri("https://orders.example/api/"), string.Empty);

    [Fact] void should_use_the_base_address() => _result.ShouldEqual(new Uri("https://orders.example/api/"));
}
