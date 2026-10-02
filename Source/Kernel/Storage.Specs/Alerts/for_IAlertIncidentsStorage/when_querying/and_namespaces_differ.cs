// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Alerts;

namespace Cratis.Chronicle.Storage.Alerts.for_IAlertIncidentsStorage.when_querying;

/// <summary>
/// Specifies and namespaces differ.
/// </summary>
/// <typeparam name="THarness">The provider harness.</typeparam>
public abstract class and_namespaces_differ<THarness> : given.an_incident_store<THarness>
    where THarness : IAlertIncidentsStorageHarness, new()
{
    AlertIncidentStoragePage _page;
    AlertIncident? _wrongStore;
    AlertIncident? _wrongNamespace;

    async Task Establish()
    {
        await _storage.Apply(Raise(1, new IncidentId(Guid.NewGuid()), @namespace: "other"));
        await _storage.Apply(Raise(2));
    }

    async Task Because()
    {
        _page = await _storage.GetOpenPage(new(new("store", EventStoreNamespaceName.Default), null, null, null), null, 1);
        _wrongStore = await _storage.GetOpen(new("other", null), _id);
        _wrongNamespace = await _storage.GetOpen(new("store", "other"), _id);
    }

    [Fact] public void should_select_exact_default_namespace() => _page.Items.Single().Id.ShouldEqual(_id);
    [Fact] public void should_scope_detail_by_store() => _wrongStore.ShouldBeNull();
    [Fact] public void should_scope_detail_by_namespace() => _wrongNamespace.ShouldBeNull();
}
