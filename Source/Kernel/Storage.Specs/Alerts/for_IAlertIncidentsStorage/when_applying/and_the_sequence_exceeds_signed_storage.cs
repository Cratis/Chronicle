// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.Alerts.for_IAlertIncidentsStorage.when_applying;

/// <summary>
/// Specifies and the sequence exceeds signed storage.
/// </summary>
/// <typeparam name="THarness">The provider harness.</typeparam>
public abstract class and_the_sequence_exceeds_signed_storage<THarness> : given.an_incident_store<THarness>
    where THarness : IAlertIncidentsStorageHarness, new()
{
    Exception? _error;

    async Task Because()
    {
        _error = await Catch.Exception(() => _storage.Apply(Raise((ulong)long.MaxValue + 1)));
    }

    [Fact] public void should_reject_with_the_common_exception() => _error.ShouldBeOfExactType<AlertIncidentSequenceNumberOutOfRange>();
}
