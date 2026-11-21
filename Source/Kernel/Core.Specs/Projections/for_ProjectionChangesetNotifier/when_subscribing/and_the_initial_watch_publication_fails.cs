// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Observation;
using Microsoft.Extensions.Logging;
using Orleans.TestKit;

namespace Cratis.Chronicle.Projections.for_ProjectionChangesetNotifier.when_subscribing;

public class and_the_initial_watch_publication_fails : Specification
{
    readonly Exception _failure = new TimeoutException("Watch publication failed");
    TestKitSilo _silo;
    ProjectionChangesetNotifier _notifier;
    IProjectionsServiceClient _client;
    Exception[] _errors;
    bool _hasSubscribers;

    async Task Establish()
    {
        _silo = new TestKitSilo();
        _silo.AddService(Substitute.For<ILogger<ProjectionChangesetNotifier>>());
        _client = _silo.AddService(Substitute.For<IProjectionsServiceClient>());
        _client.SetWatchState(Arg.Any<EventStoreName>(), Arg.Any<EventStoreNamespaceName>(), Arg.Any<ObserverId>(), true)
            .Returns(Task.FromException(_failure));
        _notifier = await _silo.CreateGrainAsync<ProjectionChangesetNotifier>(new ProjectionChangesetNotifierKey("the-projection", "the-store", "the-namespace").ToString());
    }

    async Task Because()
    {
        var subscriptions = new[]
        {
            _notifier.Subscribe(Substitute.For<IReadModelChangesetSubscriber>()),
            _notifier.Subscribe(Substitute.For<IReadModelChangesetSubscriber>())
        };
        await _silo.TimerRegistry.FireAllAsync();
        _errors = await Task.WhenAll(subscriptions.Select(subscription => Catch.Exception(() => subscription.WaitAsync(TimeSpan.FromSeconds(5)))));
        _hasSubscribers = await _notifier.HasSubscribers();
    }

    [Fact] void should_fail_every_waiting_subscription() => _errors.ShouldAllBe(_ => ReferenceEquals(_, _failure));
    [Fact] void should_remove_the_unacknowledged_subscribers() => _hasSubscribers.ShouldBeFalse();
    [Fact] void should_publish_the_rolled_back_watch_state() => _client.Received(1).SetWatchState("the-store", "the-namespace", "the-projection", false);
}
