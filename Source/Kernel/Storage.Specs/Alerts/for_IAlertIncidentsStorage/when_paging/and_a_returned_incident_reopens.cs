// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Alerts;

namespace Cratis.Chronicle.Storage.Alerts.for_IAlertIncidentsStorage.when_paging;

/// <summary>
/// Specifies and a returned incident reopens.
/// </summary>
/// <typeparam name="THarness">The provider harness.</typeparam>
public abstract class and_a_returned_incident_reopens<THarness> : given.an_incident_store<THarness>
    where THarness : IAlertIncidentsStorageHarness, new()
{
    AlertIncidentStoragePage _page;
    AlertIncidentCursor? _after;

    async Task Establish()
    {
        await _storage.Apply(Raise(1));
        await _storage.Apply(Raise(2, new IncidentId(Guid.NewGuid())));
        _after = (await _storage.EnumerateOpen(null, 1)).Next;
        await _storage.Apply(Raise(3) with { Kind = AlertIncidentTransitionKind.Cleared });
    }

    async Task Because()
    {
        await _storage.Apply(Raise(4));
        _page = await _storage.EnumerateOpen(_after, 500);
    }

    [Fact] public void should_include_the_reopened_row_at_its_new_position() => _page.Items.Last().Id.ShouldEqual(_id);
    [Fact] public void should_retain_the_other_row() => _page.Items.Count().ShouldEqual(2);
}
