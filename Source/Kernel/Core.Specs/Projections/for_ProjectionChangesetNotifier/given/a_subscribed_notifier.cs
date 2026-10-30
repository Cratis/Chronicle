// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.ReadModels;
using Microsoft.Extensions.Logging;
using Orleans.TestKit;

namespace Cratis.Chronicle.Projections.for_ProjectionChangesetNotifier.given;

public class a_subscribed_notifier : Specification
{
    protected ProjectionChangesetNotifier _notifier;
    protected readonly EventStoreNamespaceName _namespace = "the-namespace";
    protected IReadModelChangesetSubscriber[] _subscribers;
    protected ReadModelChangeNotification[] _notifications;

    async Task Establish()
    {
        var silo = new TestKitSilo();
        silo.AddService(Substitute.For<ILogger<ProjectionChangesetNotifier>>());
        _notifier = await silo.CreateGrainAsync<ProjectionChangesetNotifier>("the-projection");
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
