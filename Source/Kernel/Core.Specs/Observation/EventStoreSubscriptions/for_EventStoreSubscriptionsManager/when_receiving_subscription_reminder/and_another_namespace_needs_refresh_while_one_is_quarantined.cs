// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;

namespace Cratis.Chronicle.Observation.EventStoreSubscriptions.for_EventStoreSubscriptionsManager.when_receiving_subscription_reminder;

public class and_another_namespace_needs_refresh_while_one_is_quarantined : given.a_manager_with_a_quarantined_subscription
{
    IObserver _otherObserver;

    void Establish()
    {
        _otherObserver = Substitute.For<IObserver>();
        _otherObserver.IsSubscribed().Returns(false);
        _observer.IsSubscribed().Returns(true);
        _namespaces.GetAll().Returns([EventStoreNamespaceName.Default, new EventStoreNamespaceName("other-namespace")]);
    }

    protected override IObserver GetObserver(string identity) => identity.Contains("other-namespace", StringComparison.Ordinal) ? _otherObserver : _observer;

    async Task Because() => await _manager.ReceiveReminder(ReminderName, default);

    [Fact] void should_not_refresh_the_quarantined_namespace() => ShouldNotSubscribe(_observer);
    [Fact] void should_subscribe_the_other_namespace() => ShouldSubscribe(_otherObserver);
}
