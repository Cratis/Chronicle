// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.EventSequences;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Concepts.System;
using Cratis.Chronicle.Configuration;
using Cratis.Chronicle.Releases;
using Cratis.Chronicle.Storage;
using Cratis.Chronicle.Storage.Observation;
using Microsoft.Extensions.Options;

namespace Cratis.Chronicle.Observation.for_DurableSubscriptionRecords.given;

public class durable_subscription_records : Specification
{
    protected static readonly ObserverKey _observer = new("the-observer", "the-event-store", "the-namespace", EventSequenceId.Log);

    protected IObserverStateStorage _states;
    protected IClusterReleases _releases;
    protected ManualTime _time;
    protected Dictionary<ObserverId, ObserverDurableSubscription> _stored;
    protected DurableSubscriptionRecords _records;
    protected DurableSubscriptionMode _mode = DurableSubscriptionMode.Release;
    protected int _dormantAfter = 900;
    protected ClientKeeperMode _keeper = ClientKeeperMode.SiloLocal;
    protected bool _everySiloSupportsDormancy = true;

    protected void Create()
    {
        var storage = Substitute.For<IStorage>();
        var eventStore = Substitute.For<IEventStoreStorage>();
        var @namespace = Substitute.For<IEventStoreNamespaceStorage>();
        storage.GetEventStore(Arg.Any<EventStoreName>()).Returns(eventStore);
        eventStore.GetNamespace(Arg.Any<EventStoreNamespaceName>()).Returns(@namespace);
        _states = Substitute.For<IObserverStateStorage>();
        @namespace.Observers.Returns(_states);
        _stored = new()
        {
            [_observer.ObserverId] = new(ObserverSubscriptionKind.ClientReactor, false, null, [new("the-connection", "silo")])
        };
        _states.GetDurableSubscriptions(Arg.Any<EventSequenceId>()).Returns(_ => Task.FromResult<IReadOnlyDictionary<ObserverId, ObserverDurableSubscription>>(_stored));

        _releases = Substitute.For<IClusterReleases>();
        _releases.Summarize().Returns(_everySiloSupportsDormancy
            ? new ClusterReleaseSummary([(new SemanticVersion(99, 0, 0), 1)], 0, 0)
            : new ClusterReleaseSummary([(new SemanticVersion(19, 172, 0), 1)], 0, 0));
        _time = new();
        _records = new DurableSubscriptionRecords(
            storage,
            Options.Create(new ChronicleOptions
            {
                Observers = new Observers { DurableSubscriptions = _mode, DormantAfter = _dormantAfter },
                ConnectedClients = new Configuration.Clients { KeeperMode = _keeper }
            }),
            _releases,
            _time);
    }

    protected sealed class ManualTime : TimeProvider
    {
        DateTimeOffset _now = new(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);

        public override DateTimeOffset GetUtcNow() => _now;

        public void Advance(TimeSpan by) => _now += by;
    }
}
