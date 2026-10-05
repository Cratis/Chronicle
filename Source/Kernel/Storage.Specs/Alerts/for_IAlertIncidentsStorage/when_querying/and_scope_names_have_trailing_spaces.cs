// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.Alerts.for_IAlertIncidentsStorage.when_querying;

/// <summary>
/// Specifies ordinal scope and filter equality even on SQL Server, which normally pads strings.
/// </summary>
/// <typeparam name="THarness">The provider harness.</typeparam>
public abstract class and_scope_names_have_trailing_spaces<THarness> : given.an_incident_store<THarness>
    where THarness : IAlertIncidentsStorageHarness, new()
{
    AlertIncidentStoragePage _store;
    AlertIncidentStoragePage _namespace;
    AlertIncidentStoragePage _observer;
    AlertIncidentStoragePage _condition;

    async Task Establish() => await _storage.Apply(Raise(1));

    async Task Because()
    {
        _store = await _storage.GetOpenPage(new(new("store ", null), null, null, null), null, 100);
        _namespace = await _storage.GetOpenPage(new(new("store", "Default "), null, null, null), null, 100);
        _observer = await _storage.GetOpenPage(new(new("store", null), "observer ", null, null), null, 100);
        _condition = await _storage.GetOpenPage(new(new("store", null), null, "partition-failing ", null), null, 100);
    }

    [Fact] public void should_not_pad_affected_store_names() => _store.Items.ShouldBeEmpty();
    [Fact] public void should_not_pad_affected_namespace_names() => _namespace.Items.ShouldBeEmpty();
    [Fact] public void should_not_pad_observer_filters() => _observer.Items.ShouldBeEmpty();
    [Fact] public void should_not_pad_condition_filters() => _condition.Items.ShouldBeEmpty();
}
