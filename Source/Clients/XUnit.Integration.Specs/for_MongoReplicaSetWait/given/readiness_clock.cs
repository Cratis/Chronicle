// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.XUnit.Integration.for_MongoReplicaSetWait.given;

internal sealed class readiness_clock : TimeProvider
{
    long _timestamp;

    public override long TimestampFrequency => TimeSpan.TicksPerSecond;

    public override long GetTimestamp() => _timestamp;

    internal void Advance(TimeSpan duration) => _timestamp += duration.Ticks;
}
