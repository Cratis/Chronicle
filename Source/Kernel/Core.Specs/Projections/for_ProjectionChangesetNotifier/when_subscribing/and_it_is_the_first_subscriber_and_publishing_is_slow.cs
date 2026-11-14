// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Observation;
using Microsoft.Extensions.Logging;
using Orleans.TestKit;

namespace Cratis.Chronicle.Projections.for_ProjectionChangesetNotifier.when_subscribing;

public class and_it_is_the_first_subscriber_and_publishing_is_slow : Specification
{
    readonly ObserverId _projection = "the-projection";
    readonly EventStoreName _eventStore = "the-store";
    readonly EventStoreNamespaceName _namespace = "the-namespace";
    TestKitSilo _silo;
    ProjectionChangesetNotifier _notifier;
    TaskCompletionSource _publication;
    bool _completedBeforePublished;
    bool _completedAfterPublished;

    async Task Establish()
    {
        _silo = new TestKitSilo();
        _silo.AddService(Substitute.For<ILogger<ProjectionChangesetNotifier>>());
        _publication = new(TaskCreationOptions.RunContinuationsAsynchronously);
        var client = _silo.AddService(Substitute.For<IProjectionsServiceClient>());
        client.SetWatchState(Arg.Any<EventStoreName>(), Arg.Any<EventStoreNamespaceName>(), Arg.Any<ObserverId>(), Arg.Any<bool>()).Returns(_ => _publication.Task);
        _notifier = await _silo.CreateGrainAsync<ProjectionChangesetNotifier>(new ProjectionChangesetNotifierKey(_projection, _eventStore, _namespace).ToString());
    }

    async Task Because()
    {
        var subscription = _notifier.Subscribe(Substitute.For<IReadModelChangesetSubscriber>());
        await Task.Delay(TimeSpan.FromMilliseconds(100), TimeProvider.System);
        _completedBeforePublished = subscription.IsCompleted;
        _publication.SetResult();
        await subscription.WaitAsync(TimeSpan.FromSeconds(5), TimeProvider.System, CancellationToken.None);
        _completedAfterPublished = subscription.IsCompletedSuccessfully;
    }

    [Fact] void should_not_complete_before_the_watch_state_is_published() => _completedBeforePublished.ShouldBeFalse();
    [Fact] void should_complete_once_the_watch_state_is_published() => _completedAfterPublished.ShouldBeTrue();
}
