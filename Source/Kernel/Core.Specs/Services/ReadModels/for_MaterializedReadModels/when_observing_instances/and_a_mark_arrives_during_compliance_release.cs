// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Grpc.Core;

namespace Cratis.Chronicle.Services.ReadModels.for_MaterializedReadModels.when_observing_instances;

public class and_a_mark_arrives_during_compliance_release : given.a_revocable_observation
{
    Exception _error;

    async Task Because()
    {
        _source.OnNext([]);
        await _releaseStarted.Task;
        await _cached.MarkQuarantined();
        _release.TrySetResult([]);
        _error = await _ended.Task;
    }

    [Fact] void should_not_release_an_in_flight_page() => _delivered.ShouldEqual(0);
    [Fact] void should_end_neutrally() => _error.ShouldBeOfExactType<RpcException>();
}
