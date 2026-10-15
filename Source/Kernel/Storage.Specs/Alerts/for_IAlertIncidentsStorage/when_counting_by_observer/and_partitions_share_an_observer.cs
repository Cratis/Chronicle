// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Alerts;
using Cratis.Chronicle.Concepts.EventSequences;

namespace Cratis.Chronicle.Storage.Alerts.for_IAlertIncidentsStorage.when_counting_by_observer;

/// <summary>
/// Specifies counting open incidents by observer when partitions share an observer.
/// </summary>
/// <typeparam name="THarness">The provider harness.</typeparam>
public abstract class and_partitions_share_an_observer<THarness> : given.an_incident_store<THarness>
    where THarness : IAlertIncidentsStorageHarness, new()
{
    IEnumerable<AlertIncidentObserverCount> _counts;

    async Task Establish()
    {
        await _storage.Apply(Raise(1));
        await _storage.Apply(Raise(2, new IncidentId(Guid.NewGuid())) with { Target = Raise(2).Target with { Partition = "other-partition" } });
    }

    async Task Because() => _counts = await _storage.GetOpenCountsByObserver();

    [Fact] public void should_have_one_bucket() => _counts.Count().ShouldEqual(1);
    [Fact] public void should_count_both_partitions() => _counts.Single().Count.ShouldEqual(2L);
    [Fact] public void should_carry_the_observer_and_sequence() => (_counts.Single().ObserverId.Value, _counts.Single().EventSequenceId.Value).ShouldEqual(("observer", EventSequenceId.Log.Value));
}
