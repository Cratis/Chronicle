// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Alerts;

namespace Cratis.Chronicle.Storage.Alerts.for_IAlertIncidentsStorage.when_paging;

/// <summary>
/// Specifies and raise positions are tied.
/// </summary>
/// <typeparam name="THarness">The provider harness.</typeparam>
public abstract class and_raise_positions_are_tied<THarness> : given.an_incident_store<THarness>
    where THarness : IAlertIncidentsStorageHarness, new()
{
    List<IncidentId> _ids = [];
    AlertIncidentCursor? _last;

    async Task Establish()
    {
        await _storage.Apply(Raise(10, new IncidentId(Guid.Parse("ffffffff-ffff-ffff-ffff-ffffffffffff"))));
        await _storage.Apply(Raise(10));
        await _storage.Apply(Raise(10, new IncidentId(Guid.Parse("00000001-0000-0000-0000-000000000000"))));
    }

    async Task Because()
    {
        do
        {
            var page = await _storage.GetOpenPage(new(new("store", null), null, null, null), _last, 1);
            _ids.AddRange(page.Items.Select(row => row.Id));
            _last = page.Next;
        } while (_last is not null);
    }

    [Fact] public void should_traverse_every_tied_row_once() => _ids.Count.ShouldEqual(3);
    [Fact] public void should_start_with_the_canonical_smallest_key() => _ids[0].ShouldEqual(_id);
    [Fact] public void should_use_ordinal_not_native_guid_order() => AlertIncidentStorageRules.Key(_ids[1]).ShouldEqual("00000001000000000000000000000000");
    [Fact] public void should_finish_without_a_continuation() => _last.ShouldBeNull();
}
