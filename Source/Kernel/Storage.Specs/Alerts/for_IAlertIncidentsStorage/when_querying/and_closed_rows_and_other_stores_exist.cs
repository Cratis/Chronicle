// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Alerts;

namespace Cratis.Chronicle.Storage.Alerts.for_IAlertIncidentsStorage.when_querying;

/// <summary>
/// Specifies and closed rows and other stores exist.
/// </summary>
/// <typeparam name="THarness">The provider harness.</typeparam>
public abstract class and_closed_rows_and_other_stores_exist<THarness> : given.an_incident_store<THarness>
    where THarness : IAlertIncidentsStorageHarness, new()
{
    IEnumerable<AlertIncidentCount> _counts;
    AlertIncidentStoragePage _page;

    async Task Establish()
    {
        await _storage.Apply(Raise(1));
        await _storage.Apply(Raise(2, new IncidentId(Guid.NewGuid())));
        var closed = new IncidentId(Guid.NewGuid());
        await _storage.Apply(Raise(3, closed));
        await _storage.Apply(Raise(4, closed) with { Kind = AlertIncidentTransitionKind.Cleared });
        await _storage.Apply(Raise(5, new IncidentId(Guid.NewGuid()), store: "other"));
    }

    async Task Because()
    {
        _counts = await _storage.GetOpenCounts(new("store", null));
        _page = await _storage.GetOpenPage(new(new("store", null), null, null, null), null, 1);
    }

    [Fact] public void should_count_the_entire_matching_population() => _counts.Single().Count.ShouldEqual(2L);
    [Fact] public void should_bound_the_page_independently() => _page.Items.Count().ShouldEqual(1);
}
