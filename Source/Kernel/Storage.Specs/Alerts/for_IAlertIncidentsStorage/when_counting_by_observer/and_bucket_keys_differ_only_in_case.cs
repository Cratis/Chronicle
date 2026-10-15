// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Alerts;

namespace Cratis.Chronicle.Storage.Alerts.for_IAlertIncidentsStorage.when_counting_by_observer;

/// <summary>
/// Specifies counting open incidents by observer when bucket keys differ only in case.
/// </summary>
/// <typeparam name="THarness">The provider harness.</typeparam>
public abstract class and_bucket_keys_differ_only_in_case<THarness> : given.an_incident_store<THarness>
    where THarness : IAlertIncidentsStorageHarness, new()
{
    IEnumerable<AlertIncidentObserverCount> _counts;

    async Task Establish()
    {
        await _storage.Apply(Raise(1));
        await _storage.Apply(Raise(2, new IncidentId(Guid.NewGuid()), store: "STORE"));
        await _storage.Apply(Raise(3, new IncidentId(Guid.NewGuid()), @namespace: "DEFAULT"));
        await _storage.Apply(Raise(4, new IncidentId(Guid.NewGuid())) with { Target = Raise(4).Target with { ObserverId = "Observer" } });
    }

    async Task Because() => _counts = await _storage.GetOpenCountsByObserver();

    [Fact] public void should_keep_all_exact_buckets() => _counts.Count().ShouldEqual(4);
}
