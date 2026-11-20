// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Observation;

namespace Cratis.Chronicle.Services.Observation.for_ObserverInformationConverters.when_converting_to_contract;

public class and_the_observer_recorded_a_subscription : Specification
{
    Contracts.Observation.ObserverInformation _subscribed;
    Contracts.Observation.ObserverInformation _unsubscribed;

    void Because()
    {
        _subscribed = new ObserverDefinition().ToContract(new ObserverState
        {
            DurableSubscription = new(ObserverSubscriptionKind.ClientReactor, false, null, [new("a-connection", "silo")])
        });
        _unsubscribed = new ObserverDefinition().ToContract(new ObserverState());
    }

    [Fact] void should_report_it_as_subscribed_without_asking_the_observer() => _subscribed.IsSubscribed.ShouldBeTrue();
    [Fact] void should_not_report_an_observer_without_a_record_as_subscribed() => _unsubscribed.IsSubscribed.ShouldBeFalse();
}
