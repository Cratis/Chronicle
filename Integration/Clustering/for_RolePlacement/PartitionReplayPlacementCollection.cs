// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Integration.Clustering.for_RolePlacement;

// Each collection owns a fresh fixture, cluster and MongoDB instance. Partition replay history must not
// become input to the full-replay scenario, regardless of which scenario xUnit schedules first.
[CollectionDefinition(Name)]
public class PartitionReplayPlacementCollection : ICollectionFixture<ReplayPlacementFixture>
{
    public const string Name = "partition-replay-placement";
}
