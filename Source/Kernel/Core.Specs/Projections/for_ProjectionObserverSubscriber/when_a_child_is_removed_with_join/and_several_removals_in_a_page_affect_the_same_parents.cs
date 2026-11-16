// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Concepts.ReadModels;
using Cratis.Chronicle.Projections.Engine;

namespace Cratis.Chronicle.Projections.for_ProjectionObserverSubscriber.when_a_child_is_removed_with_join;

public class and_several_removals_in_a_page_affect_the_same_parents : given.a_run_with_affected_parents
{
    AppendedEvent _secondRemoval;
    IReadOnlyList<ReadModelChangeNotification> _notifications;

    void Establish()
    {
        _secondRemoval = _event with { Context = _event.Context with { SequenceNumber = _event.Context.SequenceNumber + 1 } };
        var context = new ProjectionEventContext("the-removed-group", _secondRemoval, _changeset, ProjectionOperationType.Join | ProjectionOperationType.Remove, false);
        context.AddAffectedParentKeys([_firstParent, _secondParent]);
        _cachedPipeline.Handle(_secondRemoval).Returns(context);
    }

    async Task Because()
    {
        _notifier.When(_ => _.NotifyMany(Arg.Any<EventStoreNamespaceName>(), Arg.Any<IReadOnlyList<ReadModelChangeNotification>>()))
            .Do(call => _notifications = call.Arg<IReadOnlyList<ReadModelChangeNotification>>());
        await _subscriber.OnNextPage([_event, _secondRemoval], new(null));
        await _silo.TimerRegistry.FireAllAsync();
        await _silo.TimerRegistry.FireAllAsync();
    }

    [Fact] void should_notify_the_parents_in_one_call() => _notifier.Received(1).NotifyMany(Arg.Any<EventStoreNamespaceName>(), Arg.Any<IReadOnlyList<ReadModelChangeNotification>>());
    [Fact] void should_notify_each_parent_exactly_once() => _notifications.Select(_ => _.ReadModelKey.Value).ShouldContainOnly(["user-1", "user-2"]);
    [Fact] void should_notify_no_more_than_the_parents() => _notifications.Count.ShouldEqual(2);
    [Fact] void should_read_the_parents_once() => _cachedPipeline.Received(1).ReadCurrent(Arg.Any<IReadOnlyCollection<Key>>());
    [Fact] void should_carry_the_last_removal() => _notifications.All(_ => _.Change.EventSequenceNumber == _secondRemoval.Context.SequenceNumber).ShouldBeTrue();
}
