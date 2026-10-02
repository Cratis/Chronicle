// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Alerts;

namespace Cratis.Chronicle.Storage.Alerts.for_IAlertIncidentsStorage.when_paging;

/// <summary>
/// Specifies and positions reach the signed maximum.
/// </summary>
/// <typeparam name="THarness">The provider harness.</typeparam>
public abstract class and_positions_reach_the_signed_maximum<THarness> : given.an_incident_store<THarness>
    where THarness : IAlertIncidentsStorageHarness, new()
{
    AlertIncidentStoragePage _page;

    async Task Establish()
    {
        await _storage.Apply(Raise(long.MaxValue));
        await _storage.Apply(Raise((ulong)long.MaxValue - 1, new IncidentId(Guid.NewGuid())));
    }

    async Task Because()
    {
        _page = await _storage.GetOpenPage(new(new("store", null), null, null, null), null, 2);
    }

    [Fact] public void should_round_trip_the_maximum() => _page.Items.Last().RaisedSequenceNumber.Value.ShouldEqual((ulong)long.MaxValue);
    [Fact] public void should_order_near_the_maximum() => _page.Items.First().RaisedSequenceNumber.Value.ShouldEqual((ulong)long.MaxValue - 1);
}
