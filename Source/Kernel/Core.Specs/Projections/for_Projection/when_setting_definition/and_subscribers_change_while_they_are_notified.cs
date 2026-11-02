// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Projections;
using Cratis.Chronicle.Concepts.Projections.Definitions;
using Cratis.Chronicle.Projections.Engine;

namespace Cratis.Chronicle.Projections.for_Projection.when_setting_definition;

/// <summary>
/// Subscribing and unsubscribing interleave with a definition change that is notifying. The observer set is
/// copy-on-write while it is notified: the round in flight keeps its snapshot, and a later round sees the new set.
/// </summary>
public class and_subscribers_change_while_they_are_notified : given.a_projection_adding_historical_event_mappings
{
    readonly TaskCompletionSource _notificationStarted = new(TaskCreationOptions.RunContinuationsAsynchronously);
    readonly TaskCompletionSource _release = new(TaskCreationOptions.RunContinuationsAsynchronously);
    INotifyProjectionDefinitionsChanged _lateSubscriber;
    bool _firstChangeCompleted;
    bool _secondChangeCompleted;
    int _firstCall;

    void Establish()
    {
        _comparer.Compare(Arg.Any<ProjectionKey>(), Arg.Any<ProjectionDefinition>(), Arg.Any<ProjectionDefinition>())
            .Returns(ProjectionDefinitionCompareResult.New);
        _lateSubscriber = Substitute.For<INotifyProjectionDefinitionsChanged>();
        _subscriber.OnProjectionDefinitionsChanged(Arg.Any<ProjectionDefinition>()).Returns(_ =>
        {
            if (Interlocked.Exchange(ref _firstCall, 1) == 0)
            {
                _notificationStarted.TrySetResult();
                return _release.Task;
            }

            return Task.CompletedTask;
        });
    }

    async Task Because()
    {
        var change = _grain.SetDefinition(_candidate);
        await _notificationStarted.Task.WaitAsync(TimeSpan.FromSeconds(10), TimeProvider.System);
        await _grain.SubscribeDefinitionsChanged(_lateSubscriber);
        await _grain.UnsubscribeDefinitionsChanged(_subscriber);
        _release.SetResult();
        await change.WaitAsync(TimeSpan.FromSeconds(10), TimeProvider.System);
        _firstChangeCompleted = true;

        await _grain.SetDefinition(_candidate with { IsRewindable = !_candidate.IsRewindable });
        _secondChangeCompleted = true;
    }

    [Fact] void should_complete_the_definition_change() => _firstChangeCompleted.ShouldBeTrue();
    [Fact] void should_complete_the_next_definition_change() => _secondChangeCompleted.ShouldBeTrue();
    [Fact] void should_notify_the_unsubscribed_subscriber_only_once() => _ = _subscriber.Received(1).OnProjectionDefinitionsChanged(Arg.Any<ProjectionDefinition>());
    [Fact] void should_notify_the_late_subscriber_only_on_the_next_change() => _ = _lateSubscriber.Received(1).OnProjectionDefinitionsChanged(Arg.Any<ProjectionDefinition>());
}
