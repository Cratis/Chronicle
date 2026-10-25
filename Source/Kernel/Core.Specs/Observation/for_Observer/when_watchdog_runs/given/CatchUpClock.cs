// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Observation.for_Observer.when_watchdog_runs.given;

public sealed class CatchUpClock : TimeProvider
{
    DateTimeOffset _now = DateTimeOffset.UnixEpoch;

    public override DateTimeOffset GetUtcNow() => _now;

    public void Advance(TimeSpan elapsed) => _now += elapsed;
}
