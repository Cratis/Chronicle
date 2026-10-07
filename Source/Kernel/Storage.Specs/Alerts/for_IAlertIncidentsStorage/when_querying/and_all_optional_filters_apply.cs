// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Alerts;

namespace Cratis.Chronicle.Storage.Alerts.for_IAlertIncidentsStorage.when_querying;

/// <summary>
/// Specifies and all optional filters apply.
/// </summary>
/// <typeparam name="THarness">The provider harness.</typeparam>
public abstract class and_all_optional_filters_apply<THarness> : given.an_incident_store<THarness>
    where THarness : IAlertIncidentsStorageHarness, new()
{
    AlertIncidentStoragePage _page;

    async Task Establish()
    {
        await _storage.Apply(Raise(1));
        await _storage.Apply(Raise(2) with { Kind = AlertIncidentTransitionKind.Escalated, Severity = AlertSeverity.Critical });
        await _storage.Apply(Raise(3, new IncidentId(Guid.NewGuid())));
    }

    async Task Because()
    {
        _page = await _storage.GetOpenPage(new(new("store", null), "observer", AlertConditionKind.PartitionFailing, AlertSeverity.Critical), null, 10);
    }

    [Fact] public void should_apply_all_filters_before_paging() => _page.Items.Single().Id.ShouldEqual(_id);
}
