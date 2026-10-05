// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.Alerts.for_IAlertIncidentsStorage.when_querying;

/// <summary>
/// Specifies and scope differs only in case.
/// </summary>
/// <typeparam name="THarness">The provider harness.</typeparam>
public abstract class and_scope_differs_only_in_case<THarness> : given.an_incident_store<THarness>
    where THarness : IAlertIncidentsStorageHarness, new()
{
    AlertIncidentStoragePage _page;
    IEnumerable<AlertIncidentCount> _counts;

    async Task Establish()
    {
        await _storage.Apply(Raise(1, store: "Store"));
    }

    async Task Because()
    {
        _page = await _storage.GetOpenPage(new(new("store", null), null, null, null), null, 10);
        _counts = await _storage.GetOpenCounts(new("store", null));
    }

    [Fact] public void should_use_ordinal_scope_equality() => _page.Items.ShouldBeEmpty();
    [Fact] public void should_scope_counts_with_ordinal_equality() => _counts.ShouldBeEmpty();
}
