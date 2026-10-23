// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.MongoDB.EventSequences;

namespace Cratis.Chronicle.Storage.MongoDB.Generations.Fencing.for_EpochCheckedDdl.when_running_a_schema_operation;

[Collection(ReplicaSetMongoDBCollection.Name)]
public class and_a_revocation_races_it(ReplicaSetMongoDBFixture fixture) : given.a_fenced_writer_against_a_replica_set(fixture)
{
    Exception? _error;

    async Task Because() =>
        _error = await Catch.Exception(() => new EpochCheckedDdl(_gates).Run(_token, async () =>
        {
            await _database.CreateCollectionAsync("renamed-target");
            await Revoke();
        }));

    [Fact] void should_report_the_race() => _error.ShouldBeOfExactType<EpochCheckedDdlRacedRevocation>();
}
