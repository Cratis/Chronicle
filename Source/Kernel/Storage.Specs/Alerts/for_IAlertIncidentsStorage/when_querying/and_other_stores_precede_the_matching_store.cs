// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Alerts;

namespace Cratis.Chronicle.Storage.Alerts.for_IAlertIncidentsStorage.when_querying;

/// <summary>
/// Specifies and other stores precede the matching store.
/// </summary>
/// <typeparam name="THarness">The provider harness.</typeparam>
public abstract class and_other_stores_precede_the_matching_store<THarness> : given.an_incident_store<THarness>
    where THarness : IAlertIncidentsStorageHarness, new()
{
    AlertIncidentStoragePage _page;

    async Task Establish()
    {
        await _storage.Apply(Raise(1, new IncidentId(Guid.NewGuid()), "other"));
        await _storage.Apply(Raise(2));
    }

    async Task Because()
    {
        _page = await _storage.GetOpenPage(new(new("store", null), null, null, null), null, 1);
    }

    [Fact] public void should_scope_before_limiting() => _page.Items.Single().Id.ShouldEqual(_id);
    [Fact] public void should_not_offer_a_spurious_continuation() => _page.Next.ShouldBeNull();
}
