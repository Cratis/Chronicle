// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Alerts;

namespace Cratis.Chronicle.Storage.Alerts.for_IAlertIncidentsStorage.when_applying;

/// <summary>
/// Specifies and the clear precedes a delayed raise.
/// </summary>
/// <typeparam name="THarness">The provider harness.</typeparam>
public abstract class and_the_clear_precedes_a_delayed_raise<THarness> : given.an_incident_store<THarness>
    where THarness : IAlertIncidentsStorageHarness, new()
{
    AlertIncidentWriteOutcome _outcome;
    AlertIncident? _row;

    async Task Establish()
    {
        await _storage.Apply(Raise(11) with { Kind = AlertIncidentTransitionKind.Cleared, Severity = null, Evidence = null, ClearedReason = AlertClearedReason.Recovered });
    }

    async Task Because()
    {
        _outcome = await _storage.Apply(Raise(10));
        _row = await _storage.GetOpen(new("store", null), _id);
    }

    [Fact] public void should_retain_the_tombstone_guard() => _outcome.ShouldEqual(AlertIncidentWriteOutcome.AlreadyApplied);
    [Fact] public void should_exclude_the_closed_row() => _row.ShouldBeNull();
}
