// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.MongoDB.EventSequences;

namespace Cratis.Chronicle.Storage.MongoDB.Generations.Fencing.for_FenceGates.when_claiming;

[Collection(ReplicaSetMongoDBCollection.Name)]
public class and_two_owners_race(ReplicaSetMongoDBFixture fixture) : given.a_fenced_writer_against_a_replica_set(fixture)
{
    FenceToken[] _claims;
    long? _current;

    async Task Because()
    {
        _claims = await Task.WhenAll(_gates.Claim(MaterializationKey, Generation), _gates.Claim(MaterializationKey, Generation));
        _current = await _gates.CurrentEpoch(MaterializationKey, Generation);
    }

    [Fact] void should_give_each_owner_its_own_epoch() => _claims[0].Epoch.ShouldNotEqual(_claims[1].Epoch);
    [Fact] void should_leave_the_last_claim_as_the_current_epoch() => _current.ShouldEqual(_claims.Max(claim => claim.Epoch));
    [Fact] void should_have_moved_past_the_first_owner() => _current.ShouldEqual(_token.Epoch + 2);
}
