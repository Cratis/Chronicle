// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Alerts;
using Cratis.Chronicle.Concepts.EventSequences;

namespace Cratis.Chronicle.Storage.Alerts.for_IAlertIncidentsStorage.when_counting_by_observer;

/// <summary>
/// Specifies counting open incidents by observer when bucket keys differ by trailing spaces.
/// </summary>
/// <typeparam name="THarness">The provider harness.</typeparam>
public abstract class and_bucket_keys_differ_by_trailing_spaces<THarness> : given.an_incident_store<THarness>
    where THarness : IAlertIncidentsStorageHarness, new()
{
    IEnumerable<AlertIncidentObserverCount> _counts;

    async Task Establish()
    {
        await _storage.Apply(Raise(1));
        await _storage.Apply(Raise(2, new IncidentId(Guid.NewGuid()), @namespace: "Default "));
        await _storage.Apply(Raise(3, new IncidentId(Guid.NewGuid()), store: "store "));
        await _storage.Apply(Raise(4, new IncidentId(Guid.NewGuid())) with { Condition = "partition-failing " });
        await _storage.Apply(Raise(5, new IncidentId(Guid.NewGuid())) with { Target = Raise(5).Target with { ObserverId = "observer " } });
        await _storage.Apply(Raise(6, new IncidentId(Guid.NewGuid())) with { Target = Raise(6).Target with { EventSequenceId = new(EventSequenceId.Log.Value + " ") } });
    }

    async Task Because() => _counts = await _storage.GetOpenCountsByObserver();

    [Fact] public void should_keep_all_exact_buckets() => _counts.Count().ShouldEqual(6);
    [Fact] public void should_not_merge_any_bucket() => _counts.All(bucket => bucket.Count == 1).ShouldBeTrue();
}
