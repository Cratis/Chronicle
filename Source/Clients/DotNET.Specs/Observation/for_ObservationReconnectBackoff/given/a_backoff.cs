// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Observation.for_ObservationReconnectBackoff.given;

public class a_backoff : Specification
{
    protected static readonly TimeSpan _baseDelay = TimeSpan.FromSeconds(2);
    protected static readonly TimeSpan _maxDelay = TimeSpan.FromSeconds(60);
    protected static readonly TimeSpan _healthyStreamDuration = TimeSpan.FromSeconds(30);

    protected ManualTime _time;
    protected double _jitter = 1;
    internal ObservationReconnectBackoff _backoff;

    void Establish()
    {
        _time = new();
        _backoff = new(_baseDelay, _maxDelay, _healthyStreamDuration, _time, () => _jitter);
    }

    /// <summary>
    /// Opens a stream that ends at once, and returns the wait before the next one.
    /// </summary>
    /// <returns>The wait.</returns>
    protected TimeSpan StreamEndsAtOnce()
    {
        _backoff.StreamOpened();
        return _backoff.NextDelay();
    }

    protected class ManualTime : TimeProvider
    {
        long _ticks;

        public override long TimestampFrequency => TimeSpan.TicksPerSecond;

        public override long GetTimestamp() => _ticks;

        public void Advance(TimeSpan by) => _ticks += by.Ticks;
    }
}
