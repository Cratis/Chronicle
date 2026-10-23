// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.MongoDB.EventSequences;

namespace Cratis.Chronicle.Storage.MongoDB.Generations.Fencing.for_FencedBatchExecutor.when_revoking;

[Collection(ReplicaSetMongoDBCollection.Name)]
public class and_the_batch_is_paused_before_commit(ReplicaSetMongoDBFixture fixture) : and_a_batch_is_paused_across_revocation(fixture, FencePausePoint.BeforeCommit);
