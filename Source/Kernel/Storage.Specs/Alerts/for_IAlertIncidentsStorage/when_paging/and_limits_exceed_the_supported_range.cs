// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Alerts;

namespace Cratis.Chronicle.Storage.Alerts.for_IAlertIncidentsStorage.when_paging;

/// <summary>
/// Specifies and limits exceed the supported range.
/// </summary>
/// <typeparam name="THarness">The provider harness.</typeparam>
public abstract class and_limits_exceed_the_supported_range<THarness> : given.an_incident_store<THarness>
    where THarness : IAlertIncidentsStorageHarness, new()
{
    AlertIncidentStoragePage _minimum;
    AlertIncidentStoragePage _maximum;

    async Task Establish()
    {
        await _storage.Apply(Raise(1));
        await _storage.Apply(Raise(2, new IncidentId(Guid.NewGuid())));
    }

    async Task Because()
    {
        _minimum = await _storage.EnumerateOpen(null, 0);
        _maximum = await _storage.EnumerateOpen(null, int.MaxValue);
    }

    [Fact] public void should_bound_the_minimum_to_one() => _minimum.Items.Count().ShouldEqual(1);
    [Fact] public void should_return_the_complete_small_population() => _maximum.Items.Count().ShouldEqual(2);
}
