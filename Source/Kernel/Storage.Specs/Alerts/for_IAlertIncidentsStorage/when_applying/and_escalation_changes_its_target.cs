// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Alerts;

namespace Cratis.Chronicle.Storage.Alerts.for_IAlertIncidentsStorage.when_applying;

/// <summary>
/// Specifies and escalation changes its target.
/// </summary>
/// <typeparam name="THarness">The provider harness.</typeparam>
public abstract class and_escalation_changes_its_target<THarness> : given.an_incident_store<THarness>
    where THarness : IAlertIncidentsStorageHarness, new()
{
    AlertIncident? _row;

    async Task Establish()
    {
        await _storage.Apply(Raise(1));
    }

    async Task Because()
    {
        await _storage.Apply(Raise(2, store: "other") with { Kind = AlertIncidentTransitionKind.Escalated, Severity = AlertSeverity.Critical, Condition = "future-condition" });
        _row = await _storage.GetOpen(new("store", null), _id);
    }

    [Fact] public void should_retain_the_original_target() => _row.Target.ShouldEqual(Raise(1).Target);
    [Fact] public void should_preserve_unknown_condition() => _row.Condition.Value.ShouldEqual("future-condition");
    [Fact] public void should_preserve_evidence_and_timestamp() => _row.Evidence.ShouldEqual(Raise(2).Evidence);
    [Fact] public void should_keep_the_literal_none_partition() => _row.Target.Partition.Value.ShouldEqual("[none]");
}
