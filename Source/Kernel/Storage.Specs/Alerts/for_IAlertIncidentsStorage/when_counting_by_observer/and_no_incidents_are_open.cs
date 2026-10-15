// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.Alerts.for_IAlertIncidentsStorage.when_counting_by_observer;

/// <summary>
/// Specifies counting open incidents by observer when no incidents are open.
/// </summary>
/// <typeparam name="THarness">The provider harness.</typeparam>
public abstract class and_no_incidents_are_open<THarness> : given.an_incident_store<THarness>
    where THarness : IAlertIncidentsStorageHarness, new()
{
    IEnumerable<AlertIncidentObserverCount> _counts;

    async Task Establish()
    {
        await _storage.Apply(Raise(1));
        await _storage.Apply(Raise(2) with { Kind = AlertIncidentTransitionKind.Cleared });
    }

    async Task Because() => _counts = await _storage.GetOpenCountsByObserver();

    [Fact] public void should_return_no_buckets() => _counts.ShouldBeEmpty();
}
