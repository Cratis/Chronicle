// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Concepts.ReadModels;
using Microsoft.Extensions.Logging;
using Orleans.TestKit;

namespace Cratis.Chronicle.Projections.for_ProjectionChangesetNotifier.given;

public class a_subscribed_notifier : Specification
{
    protected TestKitSilo _silo;
    protected ProjectionChangesetNotifier _notifier;
    protected IProjectionsServiceClient _projectionsServiceClient;
    protected SettableTimeProvider _timeProvider;
    protected readonly ObserverId _projection = "the-projection";
    protected readonly EventStoreName _eventStore = "the-store";
    protected readonly EventStoreNamespaceName _namespace = "the-namespace";
    protected IReadModelChangesetSubscriber[] _subscribers;
    protected ReadModelChangeNotification[] _notifications;

    async Task Establish()
    {
        var silo = _silo = new TestKitSilo();
        silo.AddService(Substitute.For<ILogger<ProjectionChangesetNotifier>>());
        _projectionsServiceClient = silo.AddService(Substitute.For<IProjectionsServiceClient>());
        _timeProvider = new SettableTimeProvider();
        silo.AddService<TimeProvider>(_timeProvider);
        _notifier = await silo.CreateGrainAsync<ProjectionChangesetNotifier>(new ProjectionChangesetNotifierKey(_projection, _eventStore, _namespace).ToString());
        _subscribers = [Substitute.For<IReadModelChangesetSubscriber>(), Substitute.For<IReadModelChangesetSubscriber>()];
        foreach (var subscriber in _subscribers)
        {
            await _notifier.Subscribe(subscriber);
        }

        _notifications =
        [
            new("parent", new JsonObject { ["value"] = "first" }, new(ReadModelChangeType.Added, 1, DateTimeOffset.UnixEpoch, CorrelationId.New())),
            new("child", new JsonObject { ["value"] = "second" }, new(ReadModelChangeType.Modified, 2, DateTimeOffset.UnixEpoch, CorrelationId.New())),
            new("parent", new JsonObject { ["value"] = "third" }, new(ReadModelChangeType.Removed, 3, DateTimeOffset.UnixEpoch, CorrelationId.New()))
        ];
    }
}
