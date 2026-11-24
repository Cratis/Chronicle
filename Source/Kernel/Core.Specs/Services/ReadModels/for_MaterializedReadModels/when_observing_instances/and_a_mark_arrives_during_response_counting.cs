// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Services.ReadModels.for_MaterializedReadModels.when_observing_instances;

public class and_a_mark_arrives_during_response_counting : given.a_revocable_observation
{
    Exception _error;

    async Task Because()
    {
        _release.TrySetResult([]);
        _source.OnNext([]);
        await _countStarted.Task;
        await _cached.MarkQuarantined();
        _count.TrySetResult(new([], 42));
        _error = await _ended.Task;
    }

    [Fact] void should_not_emit_the_buffered_response() => _delivered.ShouldEqual(0);
    [Fact] void should_not_emit_a_total_count_after_revocation() => _error.ShouldBeOfExactType<Grpc.Core.RpcException>();
}
