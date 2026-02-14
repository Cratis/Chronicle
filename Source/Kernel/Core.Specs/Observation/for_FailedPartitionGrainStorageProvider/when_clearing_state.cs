// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Observation;
namespace Cratis.Chronicle.Observation.for_FailedPartitionGrainStorageProvider;

public class when_clearing_state : given.the_provider
{
    static Exception _error;

    async Task Because() => _error = await Catch.Exception(() => provider.ClearStateAsync("name", GrainId.Create("type", "key"), new GrainState<FailedPartition>()));

    [Fact] void should_not_fail() => _error.ShouldBeNull();
    [Fact] void should_not_do_anything() => storage.ReceivedCalls().ShouldBeEmpty();
}
