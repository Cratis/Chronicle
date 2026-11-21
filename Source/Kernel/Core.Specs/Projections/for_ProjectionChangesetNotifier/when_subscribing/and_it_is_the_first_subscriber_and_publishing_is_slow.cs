// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Concepts.ReadModels;
using Microsoft.Extensions.Logging;
using Orleans.Concurrency;
using Orleans.TestKit;

namespace Cratis.Chronicle.Projections.for_ProjectionChangesetNotifier.when_subscribing;

public class and_it_is_the_first_subscriber_and_publishing_is_slow : Specification
{
    readonly ObserverId _projection = "the-projection";
    readonly EventStoreName _eventStore = "the-store";
    readonly EventStoreNamespaceName _namespace = "the-namespace";
    readonly TaskCompletionSource _publicationEntered = new(TaskCreationOptions.RunContinuationsAsynchronously);
    readonly TaskCompletionSource _publication = new(TaskCreationOptions.RunContinuationsAsynchronously);
    TestKitSilo _silo;
    ProjectionChangesetNotifier _notifier;
    IReadModelChangesetSubscriber _subscriber;
    ReadModelChangeNotification _notification;
    bool _completedBeforePublished;
    bool _completedAfterPublished;
    bool _notificationsCompletedBeforePublished;

    async Task Establish()
    {
        _silo = new TestKitSilo();
        _silo.AddService(Substitute.For<ILogger<ProjectionChangesetNotifier>>());
        var client = _silo.AddService(Substitute.For<IProjectionsServiceClient>());
        client.SetWatchState(Arg.Any<EventStoreName>(), Arg.Any<EventStoreNamespaceName>(), Arg.Any<ObserverId>(), Arg.Any<bool>()).Returns(_ =>
        {
            _publicationEntered.SetResult();
            return _publication.Task;
        });
        _notifier = await _silo.CreateGrainAsync<ProjectionChangesetNotifier>(new ProjectionChangesetNotifierKey(_projection, _eventStore, _namespace).ToString());
        _subscriber = Substitute.For<IReadModelChangesetSubscriber>();
        _notification = new("parent", new JsonObject(), new(ReadModelChangeType.Modified, 42, DateTimeOffset.UnixEpoch, CorrelationId.New()));
    }

    async Task Because()
    {
        var subscription = _notifier.Subscribe(_subscriber);
        var publishing = _silo.TimerRegistry.FireAllAsync();
        await _publicationEntered.Task.WaitAsync(TimeSpan.FromSeconds(5));
        _completedBeforePublished = subscription.IsCompleted;
        await _notifier.Notify(_namespace, _notification.ReadModelKey, _notification.ReadModel, _notification.Change).WaitAsync(TimeSpan.FromSeconds(5));
        await _notifier.NotifyMany(_namespace, [_notification]).WaitAsync(TimeSpan.FromSeconds(5));
        _notificationsCompletedBeforePublished = !_publication.Task.IsCompleted;
        _publication.SetResult();
        await publishing.WaitAsync(TimeSpan.FromSeconds(5));
        await subscription.WaitAsync(TimeSpan.FromSeconds(5));
        _completedAfterPublished = subscription.IsCompletedSuccessfully;
    }

    [Fact] void should_not_complete_before_the_watch_state_is_published() => _completedBeforePublished.ShouldBeFalse();
    [Fact] void should_complete_once_the_watch_state_is_published() => _completedAfterPublished.ShouldBeTrue();
    [Fact] void should_deliver_notifications_while_publication_is_stalled() => _notificationsCompletedBeforePublished.ShouldBeTrue();
    [Fact] void should_deliver_both_notifications_to_the_subscriber() => _subscriber.Received(2).OnChangeset(_namespace, _notification.ReadModelKey, _notification.ReadModel, _notification.Change);
    [Fact] void should_allow_grain_calls_to_interleave_with_the_waiting_subscription() => Attribute.IsDefined(typeof(IProjectionChangesetNotifier).GetMethod(nameof(IProjectionChangesetNotifier.Subscribe))!, typeof(AlwaysInterleaveAttribute)).ShouldBeTrue();
}
