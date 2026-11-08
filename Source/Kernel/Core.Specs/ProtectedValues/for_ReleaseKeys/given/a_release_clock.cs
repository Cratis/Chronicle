// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.ProtectedValues.for_ReleaseKeys.given;

public class a_release_clock : TimeProvider
{
    long _timestamp;
    DateTimeOffset _wall = DateTimeOffset.UnixEpoch;

    public override long TimestampFrequency => TimeSpan.TicksPerSecond;
    public override long GetTimestamp() => _timestamp;
    public override DateTimeOffset GetUtcNow() => _wall;

    public void Advance(TimeSpan duration)
    {
        _timestamp += duration.Ticks;
        _wall += duration;
    }

    public void JumpWall(TimeSpan duration) => _wall += duration;
}
