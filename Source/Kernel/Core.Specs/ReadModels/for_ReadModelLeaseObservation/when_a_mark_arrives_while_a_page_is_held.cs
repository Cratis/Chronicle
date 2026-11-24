// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reactive.Subjects;
using Cratis.Chronicle.Storage.Sinks;
using Grpc.Core;

namespace Cratis.Chronicle.ReadModels.for_ReadModelLeaseObservation;

public class when_a_mark_arrives_while_a_page_is_held : for_SinkProvenanceCache.given.a_cached_database
{
    readonly TaskCompletionSource<Exception> _ended = new();
    int _delivered;
    Exception _error;

    async Task Because()
    {
        using var source = new Subject<int>();
        var binding = new ReadModelReadBinding(Substitute.For<ISink>(), null) { Lease = _lease };
        using var subscription = binding.Guard(source).Subscribe(_ => _delivered++, error => _ended.TrySetResult(error));
        _raw.Hold = true;
        _clock.Advance(TimeSpan.FromSeconds(3));
        source.OnNext(42);
        await _cached.MarkQuarantined();
        _raw.Release();
        _error = await _ended.Task;
    }

    [Fact] void should_discard_the_held_payload() => _delivered.ShouldEqual(0);
    [Fact] void should_end_only_after_observing_the_mark() => _error.ShouldBeOfExactType<RpcException>();
}
