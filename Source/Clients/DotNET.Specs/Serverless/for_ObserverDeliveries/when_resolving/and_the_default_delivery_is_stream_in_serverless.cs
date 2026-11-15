// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Observation;

namespace Cratis.Chronicle.Serverless.for_ObserverDeliveries.when_resolving;

public class and_the_default_delivery_is_stream_in_serverless : Specification
{
    ObserverDelivery _result;

    void Because() => _result = new ObserverDeliveries(new ChronicleOptions { Serverless = { Mode = ServerlessMode.Serverless, DefaultDelivery = ObserverDelivery.Stream } }).Resolve("id", typeof(object));

    [Fact] void should_resolve_the_expected_delivery() => _result.ShouldEqual(ObserverDelivery.Stream);

    [Delivery(ObserverDelivery.Http)]
    class DeclaredHttp;
}
