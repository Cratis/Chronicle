// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.Options;
using MongoDB.Bson;
using MongoDB.Driver;

namespace Cratis.Chronicle.Storage.MongoDB.Schema.for_MongoDBCapabilityProbe;

public class when_starting_concurrently : given.a_driver
{
    readonly TaskCompletionSource<string> _index = new(TaskCreationOptions.RunContinuationsAsynchronously);
    MongoDBCapabilityReport[] _reports;
    bool _bothProbesReachedIndex;

    void Establish() => _indexes.CreateOneAsync(Arg.Any<CreateIndexModel<BsonDocument>>(), Arg.Any<CreateOneIndexOptions>(), Arg.Any<CancellationToken>())
        .Returns(_index.Task);

    async Task Because()
    {
        var other = new MongoDBCapabilityProbe(_database, Options.Create(_options), _logger);
        var first = _probe.Probe(CancellationToken.None);
        var second = other.Probe(CancellationToken.None);
        _bothProbesReachedIndex = _indexes.ReceivedCalls().Count(call => call.GetMethodInfo().Name == "CreateOneAsync") == 2;
        _index.SetException(given.command_errors.Create(2, "createIndex.collation is not implemented yet"));
        _reports = await Task.WhenAll(first, second);
    }

    [Fact] void should_probe_both_indexes_before_either_finishes() => _bothProbesReachedIndex.ShouldBeTrue();
    [Fact] void should_create_independent_collections() => _createdCollections.Distinct(StringComparer.Ordinal).Count().ShouldEqual(2);
    [Fact] void should_detect_unsupported_collation_for_both_starts() => _reports.All(report => report.Collation.State == MongoDBCapabilityState.Unsupported).ShouldBeTrue();
    [Fact] void should_drop_each_owned_collection_once() => _droppedCollections.Order(StringComparer.Ordinal).SequenceEqual(_createdCollections.Order(StringComparer.Ordinal)).ShouldBeTrue();
}
