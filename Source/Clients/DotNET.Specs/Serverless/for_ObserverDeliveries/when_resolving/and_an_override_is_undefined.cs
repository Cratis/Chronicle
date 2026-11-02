// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Observation;

namespace Cratis.Chronicle.Serverless.for_ObserverDeliveries.when_resolving;

public class and_an_override_is_undefined : Specification
{
    Exception _offError;
    Exception _onError;

    void Because()
    {
        var options = new ChronicleOptions();
        options.Serverless.Observers["orders"] = new() { Delivery = (ObserverDelivery)99 };
        var deliveries = new ObserverDeliveries(options);
        _offError = Catch.Exception(() => deliveries.Resolve("orders", typeof(Observer)));
        options.Serverless.Mode = ServerlessMode.Serverless;
        _onError = Catch.Exception(() => deliveries.Resolve("orders", typeof(Observer)));
    }

    [Fact] void should_reject_the_override_when_serverless_is_off() => _offError.ShouldBeOfExactType<InvalidServerlessConfiguration>();
    [Fact] void should_reject_the_override_when_serverless_is_on() => _onError.ShouldBeOfExactType<InvalidServerlessConfiguration>();
    [Fact] void should_identify_the_observer_and_value() => _offError.Message.ShouldContain("'orders' has the undefined value 99");

    class Observer;
}
