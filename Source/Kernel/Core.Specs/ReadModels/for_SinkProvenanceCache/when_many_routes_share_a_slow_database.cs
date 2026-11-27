// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Threading.Channels;
using Cratis.Chronicle.Storage.Sinks;

namespace Cratis.Chronicle.ReadModels.for_SinkProvenanceCache;

public class when_many_routes_share_a_slow_database : given.a_cached_database
{
    const int RouteCount = 3;

    readonly Channel<bool> _starts = Channel.CreateUnbounded<bool>();
    SinkProvenanceState _state;
    bool _slow;
    Task _pending;
    Exception? _failure;

    async Task Establish()
    {
        _state = await _cached.ReadState();
        for (var index = 0; index < RouteCount; index++)
        {
            var route = Substitute.For<ISinkProvenanceStorage>();
            route.ReadState().Returns(async _ =>
            {
                if (!_slow) return _state;
                _starts.Writer.TryWrite(true);
                await Task.Delay(TimeSpan.FromSeconds(1), _clock);
                return _state;
            });
            var lease = await _cache.Bind($"route-{index}", () => route).GetLease();
            await lease.Ensure();
        }

        _slow = true;
        _clock.Advance(TimeSpan.FromSeconds(2));
    }

    async Task Because()
    {
        _pending = _lease.Ensure();
        while (!_pending.IsCompleted)
        {
            var started = _starts.Reader.WaitToReadAsync().AsTask();
            if (await Task.WhenAny(_pending, started).WaitAsync(TimeSpan.FromSeconds(5), TimeProvider.System) == _pending) break;
            while (_starts.Reader.TryRead(out _))
            {
            }

            _clock.Advance(TimeSpan.FromSeconds(1));
        }

        _failure = await Catch.Exception(() => _pending);
    }

    [Fact] void should_complete_the_pass_in_the_time_of_the_slowest_route() => _failure.ShouldBeNull();
    [Fact] void should_admit_on_the_fresh_observation() => Catch.Exception(_lease.Check).ShouldBeNull();
}
