// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Concepts.ReadModels;

namespace Cratis.Chronicle.Projections.for_ProjectionObserverSubscriber.when_a_child_is_removed_with_join;

public class and_a_parent_no_longer_exists : given.a_run_with_affected_parents
{
    IReadOnlyList<ReadModelChangeNotification> _notifications;

    protected override IReadOnlyList<(Key Key, ExpandoObject State)> CurrentParents() => [_currentParents[0]];

    async Task Because()
    {
        _notifier.When(_ => _.NotifyMany(Arg.Any<EventStoreNamespaceName>(), Arg.Any<IReadOnlyList<ReadModelChangeNotification>>()))
            .Do(call => _notifications = call.Arg<IReadOnlyList<ReadModelChangeNotification>>());
        await _subscriber.OnNext("the-removed-group", [_event], new(null));
        await _silo.TimerRegistry.FireAllAsync();
    }

    [Fact] void should_notify_only_the_parent_that_exists() => _notifications.Select(_ => _.ReadModelKey.Value).ShouldContainOnly(["user-1"]);
}
