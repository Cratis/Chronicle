// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Alerts;

namespace Cratis.Chronicle.Storage.Alerts.for_IAlertIncidentsStorage.when_querying;

public abstract class and_a_similar_store_name_holds_the_incident<THarness> : given.an_incident_store<THarness>
    where THarness : IAlertIncidentsStorageHarness, new()
{
    readonly IncidentId _otherId = new(Guid.Parse("00000000-0000-0000-0000-000000000002"));
    readonly IncidentId _ownId = new(Guid.Parse("00000000-0000-0000-0000-000000000003"));
    IEnumerable<AlertIncident> _items = [];
    List<AlertIncident?> _details = [];
    AlertIncident? _own;
    IEnumerable<AlertIncidentCount> _counts = [];

    async Task Establish()
    {
        await _storage.Apply(Raise(1, _id, store: "shop2"));
        await _storage.Apply(Raise(2, _otherId, store: "sho"));
        await _storage.Apply(Raise(3, _ownId, store: "shop"));
    }

    async Task Because()
    {
        var page = await _storage.GetOpenPage(new(new("shop", null), null, null, null), null, 10);
        _items = page.Items;
        _details = [await _storage.GetOpen(new("shop", null), _id), await _storage.GetOpen(new("shop", "Default"), _otherId), await _storage.GetOpen(new("shop ", null), _ownId)];
        _counts = await _storage.GetOpenCounts(new("shop", null));
        _own = await _storage.GetOpen(new("shop", null), _ownId);
    }

    [Fact] public void should_list_only_the_incident_of_the_exact_store_name() => _items.Single().Id.ShouldEqual(_ownId);
    [Fact] public void should_not_return_an_incident_id_of_another_store() => _details.TrueForAll(detail => detail is null).ShouldBeTrue();
    [Fact] public void should_count_only_the_incident_of_the_exact_store_name() => _counts.Sum(count => count.Count).ShouldEqual(1);
    [Fact] public void should_return_the_incident_of_the_exact_store_name_by_its_id() => _own.ShouldNotBeNull();
}
