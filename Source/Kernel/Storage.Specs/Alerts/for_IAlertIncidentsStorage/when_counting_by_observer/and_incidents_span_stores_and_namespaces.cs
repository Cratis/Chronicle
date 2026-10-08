// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Alerts;

namespace Cratis.Chronicle.Storage.Alerts.for_IAlertIncidentsStorage.when_counting_by_observer;

/// <summary>
/// Specifies counting open incidents by observer when incidents span stores and namespaces.
/// </summary>
/// <typeparam name="THarness">The provider harness.</typeparam>
public abstract class and_incidents_span_stores_and_namespaces<THarness> : given.an_incident_store<THarness>
    where THarness : IAlertIncidentsStorageHarness, new()
{
    IEnumerable<AlertIncidentObserverCount> _counts;

    async Task Establish()
    {
        await _storage.Apply(Raise(1));
        await _storage.Apply(Raise(2, new IncidentId(Guid.NewGuid()), store: "other"));
        await _storage.Apply(Raise(3, new IncidentId(Guid.NewGuid()), @namespace: "tenant"));
        var closed = new IncidentId(Guid.NewGuid());
        await _storage.Apply(Raise(4, closed));
        await _storage.Apply(Raise(5, closed) with { Kind = AlertIncidentTransitionKind.Cleared });
    }

    async Task Because() => _counts = await _storage.GetOpenCountsByObserver();

    [Fact] public void should_have_a_bucket_per_store_and_namespace() => _counts.Count().ShouldEqual(3);
    [Fact] public void should_exclude_closed_rows() => _counts.Sum(bucket => bucket.Count).ShouldEqual(3L);
}
