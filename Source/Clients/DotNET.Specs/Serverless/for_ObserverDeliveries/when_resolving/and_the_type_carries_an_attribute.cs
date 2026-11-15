// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Observation;

namespace Cratis.Chronicle.Serverless.for_ObserverDeliveries.when_resolving;

public class and_the_type_carries_an_attribute : Specification
{
    ObserverDelivery _result;

    void Because() => _result = new ObserverDeliveries(new ChronicleOptions()).Resolve("id", typeof(DeclaredHttp));

    [Fact] void should_resolve_the_expected_delivery() => _result.ShouldEqual(ObserverDelivery.Http);

    [Delivery(ObserverDelivery.Http)]
    class DeclaredHttp;
}
