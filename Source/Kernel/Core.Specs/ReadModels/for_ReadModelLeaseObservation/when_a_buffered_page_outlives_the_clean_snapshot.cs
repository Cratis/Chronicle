// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reactive.Subjects;
using Cratis.Chronicle.Storage.Sinks;

namespace Cratis.Chronicle.ReadModels.for_ReadModelLeaseObservation;

public class when_a_buffered_page_outlives_the_clean_snapshot : for_SinkProvenanceCache.given.a_cached_database
{
    readonly TaskCompletionSource<int> _delivered = new();
    Exception _error;
    bool _paused;
    int _value;

    async Task Because()
    {
        using var source = new Subject<int>();
        var binding = new ReadModelReadBinding(Substitute.For<ISink>(), null) { Lease = _lease };
        using var subscription = binding.Guard(source).Subscribe(value => _delivered.TrySetResult(value), error => _error = error);
        _raw.Hold = true;
        _clock.Advance(TimeSpan.FromSeconds(1));
        _clock.Advance(TimeSpan.FromSeconds(2));
        source.OnNext(1);
        source.OnNext(2);
        _paused = !_delivered.Task.IsCompleted;
        var refresh = _cached.ReadState();
        _raw.Release(); // This response started two seconds ago; it cannot grant a new window.
        await refresh.WaitAsync(TimeSpan.FromSeconds(5), TimeProvider.System);
        _clock.Advance(TimeSpan.FromSeconds(1));
        _value = await _delivered.Task.WaitAsync(TimeSpan.FromSeconds(5), TimeProvider.System);
    }

    [Fact] void should_hold_the_page_while_the_snapshot_is_stale() => _paused.ShouldBeTrue();
    [Fact] void should_not_end_on_expiry_alone() => _error.ShouldBeNull();
    [Fact] void should_resume_with_only_the_latest_page_after_revalidation() => _value.ShouldEqual(2);
}
