// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Serverless.for_ObserverDeliveryPlan.when_creating;

public class and_nothing_is_delivered_over_http : Specification
{
    Exception _error;

    void Because()
    {
        var options = new ChronicleOptions();
        var plan = ObserverDeliveryPlan.Create(new ObserverDeliveries(options), [("stream-one", typeof(object))]);
        _error = Catch.Exception(() => plan.ThrowIfNotDeliverable(options));
    }

    [Fact] void should_not_fail() => _error.ShouldBeNull();
}
