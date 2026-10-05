// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Alerts;

namespace Cratis.Chronicle.Storage.Alerts.for_IAlertIncidentsStorage.when_applying;

/// <summary>
/// Specifies and competing newer transitions arrive.
/// </summary>
/// <typeparam name="THarness">The provider harness.</typeparam>
public abstract class and_competing_newer_transitions_arrive<THarness> : given.an_incident_store<THarness>
    where THarness : IAlertIncidentsStorageHarness, new()
{
    AlertIncident? _row;

    async Task Establish()
    {
        await _storage.Apply(Raise(1));
    }

    async Task Because()
    {
        await Task.WhenAll(Task.Run(() => _storage.Apply(Raise(10) with { Kind = AlertIncidentTransitionKind.Escalated, Severity = AlertSeverity.Critical })), Task.Run(() => _storage.Apply(Raise(11) with { Kind = AlertIncidentTransitionKind.Escalated, Severity = AlertSeverity.Critical })));
        _row = await _storage.GetOpen(new("store", null), _id);
    }

    [Fact] public void should_keep_the_newer_transition() => _row.LastTransitionSequenceNumber.Value.ShouldEqual(11UL);
    [Fact] public void should_retain_the_original_raise() => _row.RaisedSequenceNumber.Value.ShouldEqual(1UL);
}
