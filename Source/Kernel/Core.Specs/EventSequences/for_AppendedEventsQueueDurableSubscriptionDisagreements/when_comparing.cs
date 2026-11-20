// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.EventSequences.for_AppendedEventsQueueDurableSubscriptionDisagreements;

public class when_comparing : Specification
{
    AppendedEventsQueueDurableSubscriptionDisagreements _agreeing;
    AppendedEventsQueueDurableSubscriptionDisagreements _disagreeing;

    void Because()
    {
        _agreeing = AppendedEventsQueueDurableSubscriptionDisagreements.Compare(["b", "a"], ["a", "b"]);
        _disagreeing = AppendedEventsQueueDurableSubscriptionDisagreements.Compare(["shared", "stored-b", "stored-a"], ["live", "shared"]);
    }

    [Fact] void should_agree_when_both_sets_are_the_same() => _agreeing.Agree.ShouldBeTrue();
    [Fact] void should_report_the_stored_subscriptions_that_are_not_live() => string.Join(',', _disagreeing.StoredNotLive.Select(_ => _.Value)).ShouldEqual("stored-a,stored-b");
    [Fact] void should_report_the_live_subscriptions_that_are_not_stored() => _disagreeing.LiveNotStored.Single().Value.ShouldEqual("live");
    [Fact] void should_not_agree() => _disagreeing.Agree.ShouldBeFalse();
}
