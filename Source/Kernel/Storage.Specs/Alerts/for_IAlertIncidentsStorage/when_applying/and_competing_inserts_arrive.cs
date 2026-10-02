// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.Alerts.for_IAlertIncidentsStorage.when_applying;

/// <summary>
/// Specifies and competing inserts arrive.
/// </summary>
/// <typeparam name="THarness">The provider harness.</typeparam>
public abstract class and_competing_inserts_arrive<THarness> : given.an_incident_store<THarness>
    where THarness : IAlertIncidentsStorageHarness, new()
{
    AlertIncident? _row;

    async Task Because()
    {
        await Task.WhenAll(Task.Run(() => _storage.Apply(Raise(10))), Task.Run(() => _storage.Apply(Raise(11))));
        _row = await _storage.GetOpen(new("store", null), _id);
    }

    [Fact] public void should_keep_the_newer_raise() => _row.LastTransitionSequenceNumber.Value.ShouldEqual(11UL);
}
