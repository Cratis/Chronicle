// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Observation.Alerts.for_ObserverAlerts.given;

public class ControllableTimeProvider : TimeProvider
{
    public DateTimeOffset Now { get; set; } = new(2026, 1, 1, 12, 0, 0, TimeSpan.Zero);

    public override DateTimeOffset GetUtcNow() => Now;
}
