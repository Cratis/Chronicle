// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Observation;
namespace Cratis.Chronicle.Grains.Observation.for_FailedPartitionGrainStorageProvider;

public class when_clearing_state : given.the_provider
{
    static Exception error;

    async Task Because() => error = await Catch.Exception(() => provider.ClearStateAsync("name", GrainId.Create("type", "key"), new GrainState<FailedPartition>()));

    [Fact] void should_not_fail() => error.ShouldBeNull();
}
