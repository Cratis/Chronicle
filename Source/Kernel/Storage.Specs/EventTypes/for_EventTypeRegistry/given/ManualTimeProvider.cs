// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.EventTypes.for_EventTypeRegistry.given;

/// <summary>
/// A <see cref="TimeProvider"/> whose clock only moves when told to.
/// </summary>
public class ManualTimeProvider : TimeProvider
{
    DateTimeOffset _now = new(2026, 1, 1, 12, 0, 0, TimeSpan.Zero);

    public void Advance(TimeSpan by) => _now += by;

    public override DateTimeOffset GetUtcNow() => _now;
}
