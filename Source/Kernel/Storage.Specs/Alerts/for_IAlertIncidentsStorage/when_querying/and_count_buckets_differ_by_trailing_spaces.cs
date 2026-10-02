// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Alerts;

namespace Cratis.Chronicle.Storage.Alerts.for_IAlertIncidentsStorage.when_querying;

/// <summary>
/// Specifies exact count grouping without SQL Server's padded string equality.
/// </summary>
/// <typeparam name="THarness">The provider harness.</typeparam>
public abstract class and_count_buckets_differ_by_trailing_spaces<THarness> : given.an_incident_store<THarness>
    where THarness : IAlertIncidentsStorageHarness, new()
{
    IEnumerable<AlertIncidentCount> _counts;

    async Task Establish()
    {
        await _storage.Apply(Raise(1));
        await _storage.Apply(Raise(2, new IncidentId(Guid.NewGuid()), @namespace: "Default "));
        await _storage.Apply(Raise(3, new IncidentId(Guid.NewGuid())) with { Condition = "partition-failing " });
    }

    async Task Because() => _counts = await _storage.GetOpenCounts(new("store", null));

    [Fact] public void should_keep_all_three_exact_buckets() => _counts.Count().ShouldEqual(3);
    [Fact] public void should_not_merge_any_bucket() => _counts.All(bucket => bucket.Count == 1).ShouldBeTrue();
}
