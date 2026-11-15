// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Events.Constraints;

namespace Cratis.Chronicle.Events.Constraints.StoreKeys.for_UniqueConstraintEncodingStorage;

public class when_inserting_concurrently : given.a_key_storage
{
    UniqueConstraintEncodingKey[] _candidates;
    UniqueConstraintEncodingKey[] _results;
    UniqueConstraintEncodingKey _stored;

    void Establish() => _candidates = Enumerable.Range(0, 8).Select(_ => UniqueConstraintEncodingKey.Create()).ToArray();

    async Task Because()
    {
        var start = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var attempts = _candidates.Select(candidate => Task.Run(async () =>
        {
            await start.Task;
            return await _storage.CreateKeyIfAbsent(candidate);
        })).ToArray();
        start.SetResult();
        _results = await Task.WhenAll(attempts);
        _stored = (await _storage.GetKey())!;
    }

    [Fact] void should_have_exactly_one_winning_candidate() => _candidates.Count(candidate => candidate.KeyId == _stored.KeyId).ShouldEqual(1);
    [Fact] void should_return_the_winning_pair_to_every_contender() => _results.All(result => result == _stored).ShouldBeTrue();
    [Fact] void should_preserve_the_winners_material() => Assert.Equal(_candidates.Single(candidate => candidate.KeyId == _stored.KeyId).Material.CopyBytes(), _stored.Material.CopyBytes());
}
