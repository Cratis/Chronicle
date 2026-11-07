// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.ReadModels;
using Cratis.Chronicle.Observation;

namespace Cratis.Chronicle.Projections.for_ProjectionObserverSubscriber.when_a_child_is_removed_with_join;

public class and_parents_are_affected : given.a_run_with_affected_parents
{
    ObserverSubscriberResult _result;
    bool _notifiedBeforeTheTimerFired;
    IReadOnlyList<ReadModelChangeNotification> _notifications;

    async Task Because()
    {
        _notifier.When(_ => _.NotifyMany(Arg.Any<EventStoreNamespaceName>(), Arg.Any<IReadOnlyList<ReadModelChangeNotification>>()))
            .Do(call => _notifications = call.Arg<IReadOnlyList<ReadModelChangeNotification>>());
        _result = await _subscriber.OnNext("the-removed-group", [_event], new(null));
        _notifiedBeforeTheTimerFired = _notifications is not null;
        await _silo.TimerRegistry.FireAllAsync();
    }

    [Fact] void should_succeed() => _result.State.ShouldEqual(ObserverSubscriberState.Ok);
    [Fact] void should_not_notify_before_the_acknowledgement() => _notifiedBeforeTheTimerFired.ShouldBeFalse();
    [Fact] void should_notify_each_parent() => _notifications.Select(_ => _.ReadModelKey.Value).ShouldContainOnly(["user-1", "user-2"]);
    [Fact] void should_notify_them_as_modified() => _notifications.All(_ => _.Change.ChangeType == ReadModelChangeType.Modified).ShouldBeTrue();
    [Fact] void should_not_notify_the_removal_event_partition() => _notifier.DidNotReceive().Notify(Arg.Any<EventStoreNamespaceName>(), Arg.Any<ReadModelKey>(), Arg.Any<System.Text.Json.Nodes.JsonObject>(), Arg.Any<ReadModelChangeContext>());
}
