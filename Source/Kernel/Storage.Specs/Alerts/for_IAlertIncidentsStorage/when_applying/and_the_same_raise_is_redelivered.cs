// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.Alerts.for_IAlertIncidentsStorage.when_applying;

/// <summary>
/// Specifies and the same raise is redelivered.
/// </summary>
/// <typeparam name="THarness">The provider harness.</typeparam>
public abstract class and_the_same_raise_is_redelivered<THarness> : given.an_incident_store<THarness>
    where THarness : IAlertIncidentsStorageHarness, new()
{
    AlertIncidentWriteOutcome _outcome;
    AlertIncident? _row;

    async Task Establish()
    {
        await _storage.Apply(Raise(10));
    }

    async Task Because()
    {
        _outcome = await _storage.Apply(Raise(10));
        _row = await _storage.GetOpen(new("store", null), _id);
    }

    [Fact] public void should_confirm_the_duplicate() => _outcome.ShouldEqual(AlertIncidentWriteOutcome.AlreadyApplied);
    [Fact] public void should_preserve_the_committed_row() => _row.ShouldEqual(AlertIncidentFold.Apply(null, Raise(10)).Incident);
}
