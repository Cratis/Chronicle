// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reactive.Subjects;
using Cratis.Chronicle.Storage.Sinks;
using Grpc.Core;

namespace Cratis.Chronicle.ReadModels.for_ReadModelLeaseObservation;

public class when_a_quiet_source_is_revoked : for_SinkProvenanceCache.given.a_cached_database
{
    readonly TaskCompletionSource<Exception> _ended = new();
    int _delivered;
    Exception _error;
    bool _subscribed;

    async Task Because()
    {
        using var source = new Subject<int>();
        var binding = new ReadModelReadBinding(Substitute.For<ISink>(), null) { Lease = _lease };
        using var subscription = binding.Guard(source).Subscribe(_ => _delivered++, error => _ended.TrySetResult(error));
        await _cached.MarkQuarantined();
        _error = await _ended.Task.WaitAsync(TimeSpan.FromSeconds(5));
        _subscribed = source.HasObservers;
        source.OnNext(42);
    }

    [Fact] void should_end_neutrally_without_waiting_for_a_payload_event() => _error.ShouldBeOfExactType<RpcException>();
    [Fact] void should_dispose_the_source_subscription() => _subscribed.ShouldBeFalse();
    [Fact] void should_not_emit_after_revocation() => _delivered.ShouldEqual(0);
}
