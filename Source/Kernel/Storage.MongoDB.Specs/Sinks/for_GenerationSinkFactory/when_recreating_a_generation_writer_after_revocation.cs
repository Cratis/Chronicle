// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.MongoDB.Generations.Fencing;
using Cratis.Chronicle.Storage.Sinks;
using MongoDB.Bson;
using MongoDB.Driver;

namespace Cratis.Chronicle.Storage.MongoDB.Sinks.for_GenerationSinkFactory;

public class when_recreating_a_generation_writer_after_revocation : given.a_generation_factory
{
    ISink _original;
    Exception? _error;

    void Establish() => _original = _factory.CreateFor("store", "namespace", _context);

    async Task Because()
    {
        await Writes(_original).InBatch(() => Task.FromResult(1));
        _epoch++;
        var recreated = _factory.CreateFor("store", "namespace", _context);
        _error = await Catch.Exception(() => Writes(recreated).InBatch(() => Task.FromResult(2)));
    }

    [Fact] void should_refuse_the_recreated_stale_writer() => _error.ShouldBeOfExactType<FenceRevoked>();
    [Fact] void should_keep_both_admissions_on_the_original_epoch() => _admissions.Select(document => document["epoch"].AsInt64).ShouldEqual([13L, 13L]);
    [Fact] void should_use_the_gate_revoked_by_publication_not_the_schema_generation() => _admissions.Select(document => document["_id"].AsString).Distinct().Single().ShouldEqual(_context.Grant.Gate.Value);
    [Fact] void should_never_claim_an_epoch_during_construction() => _gates.DidNotReceive().FindOneAndUpdateAsync(Arg.Any<FilterDefinition<BsonDocument>>(), Arg.Any<UpdateDefinition<BsonDocument>>(), Arg.Any<FindOneAndUpdateOptions<BsonDocument, BsonDocument>>(), Arg.Any<CancellationToken>());
}
