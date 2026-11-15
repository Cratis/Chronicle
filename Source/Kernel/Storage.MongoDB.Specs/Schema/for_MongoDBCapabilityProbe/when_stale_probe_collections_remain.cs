// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using MongoDB.Bson;

namespace Cratis.Chronicle.Storage.MongoDB.Schema.for_MongoDBCapabilityProbe;

public class when_stale_probe_collections_remain : given.a_driver
{
    readonly string _stale = $"{WellKnownCollectionNames.CapabilityProbePrefix}{ObjectId.GenerateNewId(DateTime.UtcNow.AddDays(-2))}";
    readonly string _recent = $"{WellKnownCollectionNames.CapabilityProbePrefix}{ObjectId.GenerateNewId()}";
    readonly string _unrecognized = $"{WellKnownCollectionNames.CapabilityProbePrefix}unrecognized";

    void Establish()
    {
        _collections.MoveNextAsync(Arg.Any<CancellationToken>()).Returns(Task.FromResult(true), Task.FromResult(false));
        _collections.Current.Returns([_stale, _recent, _unrecognized, MongoDBCapabilityProbe.ProbeCollectionName, "orders"]);
    }

    async Task Because() => _report = await _probe.Probe(CancellationToken.None);

    [Fact] void should_still_probe_collation() => _report.Collation.ShouldEqual(MongoDBCapability.Supported);
    [Fact] void should_remove_the_stale_probe_collection() => _droppedCollections.ShouldContain(_stale);
    [Fact] void should_remove_its_own_collection() => _droppedCollections.ShouldContain(_createdCollections.Single());
    [Fact] void should_leave_recent_and_unrecognized_collections_alone() => _droppedCollections.Count.ShouldEqual(2);
}
