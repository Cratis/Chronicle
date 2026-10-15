// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Alerts;

namespace Cratis.Chronicle.Storage.Alerts.for_IAlertIncidentsStorage.when_counting_by_observer;

/// <summary>
/// Specifies counting open incidents by observer when an incident escalates.
/// </summary>
/// <typeparam name="THarness">The provider harness.</typeparam>
public abstract class and_an_incident_escalates<THarness> : given.an_incident_store<THarness>
    where THarness : IAlertIncidentsStorageHarness, new()
{
    IEnumerable<AlertIncidentObserverCount> _counts;

    async Task Establish()
    {
        await _storage.Apply(Raise(1));
        await _storage.Apply(Raise(2) with { Kind = AlertIncidentTransitionKind.Escalated, Severity = AlertSeverity.Critical });
    }

    async Task Because() => _counts = await _storage.GetOpenCountsByObserver();

    [Fact] public void should_drop_the_warning_bucket() => _counts.Any(bucket => bucket.Severity == AlertSeverity.Warning).ShouldBeFalse();
    [Fact] public void should_have_one_critical_incident() => _counts.Single(bucket => bucket.Severity == AlertSeverity.Critical).Count.ShouldEqual(1L);
    [Fact] public void should_preserve_the_total() => _counts.Sum(bucket => bucket.Count).ShouldEqual(1L);
}
