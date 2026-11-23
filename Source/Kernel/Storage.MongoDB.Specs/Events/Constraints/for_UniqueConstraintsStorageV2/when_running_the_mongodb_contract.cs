// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.MongoDB.EventSequences;

namespace Cratis.Chronicle.Storage.MongoDB.Events.Constraints.for_UniqueConstraintsStorageV2;

[Collection(ReplicaSetMongoDBCollection.Name)]
public partial class when_running_the_mongodb_contract(ReplicaSetMongoDBFixture fixture) : given.a_mongodb_v2_storage(fixture);
