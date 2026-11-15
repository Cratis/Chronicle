// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.EventSequences.for_EventSequence.given;

public sealed class MovableClock : TimeProvider
{
    public DateTimeOffset Now { get; set; } = new(2026, 2, 3, 4, 5, 6, TimeSpan.Zero);

    public override DateTimeOffset GetUtcNow() => Now;
}
