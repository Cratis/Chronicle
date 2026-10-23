// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;

namespace Cratis.Chronicle.Integration.Clustering.for_RolePlacement;

public class when_isolating_replay_scenarios
{
    [Fact]
    void should_give_streaming_and_partition_replay_separate_collection_fixtures()
    {
        var streaming = typeof(when_replaying_a_kernel_projection_with_split_roles).GetCustomAttribute<CollectionAttribute>()!.Name;
        var partition = typeof(when_replaying_one_partition_with_split_roles).GetCustomAttribute<CollectionAttribute>()!.Name;

        streaming.ShouldNotEqual(partition);
    }
}
