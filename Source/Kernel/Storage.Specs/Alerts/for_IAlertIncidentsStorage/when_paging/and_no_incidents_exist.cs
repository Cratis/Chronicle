// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.Alerts.for_IAlertIncidentsStorage.when_paging;

/// <summary>
/// Specifies and no incidents exist.
/// </summary>
/// <typeparam name="THarness">The provider harness.</typeparam>
public abstract class and_no_incidents_exist<THarness> : given.an_incident_store<THarness>
    where THarness : IAlertIncidentsStorageHarness, new()
{
    AlertIncidentStoragePage _page;

    async Task Because()
    {
        _page = await _storage.EnumerateOpen(null, 500);
    }

    [Fact] public void should_return_an_empty_page() => _page.Items.ShouldBeEmpty();
    [Fact] public void should_not_return_a_continuation() => _page.Next.ShouldBeNull();
}
