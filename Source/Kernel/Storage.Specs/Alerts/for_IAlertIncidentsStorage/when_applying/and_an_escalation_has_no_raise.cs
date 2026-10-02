// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.Alerts.for_IAlertIncidentsStorage.when_applying;

/// <summary>
/// Specifies and an escalation has no raise.
/// </summary>
/// <typeparam name="THarness">The provider harness.</typeparam>
public abstract class and_an_escalation_has_no_raise<THarness> : given.an_incident_store<THarness>
    where THarness : IAlertIncidentsStorageHarness, new()
{
    AlertIncidentWriteOutcome _outcome;

    async Task Because()
    {
        _outcome = await _storage.Apply(Raise(10) with { Kind = AlertIncidentTransitionKind.Escalated });
    }

    [Fact] public void should_acknowledge_the_orphan() => _outcome.ShouldEqual(AlertIncidentWriteOutcome.OrphanEscalation);
}
