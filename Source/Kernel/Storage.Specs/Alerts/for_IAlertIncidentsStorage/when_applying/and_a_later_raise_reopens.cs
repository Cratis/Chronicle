// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.Alerts.for_IAlertIncidentsStorage.when_applying;

/// <summary>
/// Specifies and a later raise reopens.
/// </summary>
/// <typeparam name="THarness">The provider harness.</typeparam>
public abstract class and_a_later_raise_reopens<THarness> : given.an_incident_store<THarness>
    where THarness : IAlertIncidentsStorageHarness, new()
{
    AlertIncident? _row;

    async Task Establish()
    {
        await _storage.Apply(Raise(1));
        await _storage.Apply(Raise(2) with { Kind = AlertIncidentTransitionKind.Cleared });
    }

    async Task Because()
    {
        await _storage.Apply(Raise(3));
        _row = await _storage.GetOpen(new("store", null), _id);
    }

    [Fact] public void should_reset_raise_metadata() => _row.ShouldEqual(AlertIncidentFold.Apply(null, Raise(3)).Incident);
}
