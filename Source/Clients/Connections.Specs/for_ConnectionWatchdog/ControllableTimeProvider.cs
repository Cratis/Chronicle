// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Connections.for_ConnectionWatchdog;

public class ControllableTimeProvider : TimeProvider
{
    public DateTimeOffset Current { get; set; } = DateTimeOffset.UtcNow;

    public override DateTimeOffset GetUtcNow() => Current;

    public void Advance(TimeSpan time) => Current += time;
}
