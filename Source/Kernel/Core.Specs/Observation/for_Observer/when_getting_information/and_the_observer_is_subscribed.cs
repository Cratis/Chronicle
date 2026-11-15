// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Observation.for_Observer.when_getting_information;

public class and_the_observer_is_subscribed : given.an_observer_with_subscription
{
    ObserverInformationSnapshot _result;
    bool _isSubscribed;

    async Task Because()
    {
        _isSubscribed = await _observer.IsSubscribed();
        _result = await _observer.GetInformation();
    }

    [Fact] void should_report_the_subscription_flag_of_the_observer() => _result.IsSubscribed.ShouldEqual(_isSubscribed);
    [Fact] void should_be_subscribed() => _result.IsSubscribed.ShouldBeTrue();
}
