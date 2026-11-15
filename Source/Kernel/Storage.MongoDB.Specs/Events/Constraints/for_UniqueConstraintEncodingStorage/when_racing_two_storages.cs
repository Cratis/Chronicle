// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Events.Constraints;
using Cratis.Chronicle.Storage.MongoDB.EventSequences;

namespace Cratis.Chronicle.Storage.MongoDB.Events.Constraints.for_UniqueConstraintEncodingStorage;

[Collection(ReplicaSetMongoDBCollection.Name)]
public class when_racing_two_storages(ReplicaSetMongoDBFixture fixture) : given.a_key_storage(fixture)
{
    UniqueConstraintEncodingKey[] _candidates;
    UniqueConstraintEncodingKey[] _results;
    UniqueConstraintEncodingKey _stored;

    async Task Because()
    {
        var storages = new[] { CreateStorage(), CreateStorage() };
        _candidates = Enumerable.Range(0, 8).Select(_ => UniqueConstraintEncodingKey.Create()).ToArray();
        using var gate = new ManualResetEventSlim(false);
        var tasks = _candidates.Select((candidate, index) => Task.Run(() =>
        {
            gate.Wait();
            return storages[index % 2].CreateKeyIfAbsent(candidate);
        })).ToArray();
        gate.Set();
        _results = await Task.WhenAll(tasks);
        _stored = (await _storage.GetKey())!;
    }

    [Fact] void should_have_exactly_one_winner() => _results.Select(_ => _.KeyId.Value).Distinct().Count().ShouldEqual(1);
    [Fact] void should_return_a_submitted_candidate() => _candidates.Select(_ => _.KeyId.Value).ShouldContain(_results[0].KeyId.Value);
    [Fact] void should_store_the_winner() => _stored.KeyId.ShouldEqual(_results[0].KeyId);
    [Fact] void should_store_the_winning_material() => Assert.Equal(_results[0].Material.CopyBytes(), _stored.Material.CopyBytes());
}
