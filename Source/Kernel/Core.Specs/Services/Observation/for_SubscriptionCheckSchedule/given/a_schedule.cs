// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Services.Observation.for_SubscriptionCheckSchedule.given;

public class a_schedule : Specification
{
    protected static readonly TimeSpan Revise = TimeSpan.FromSeconds(2);
    protected static readonly TimeSpan SafetyNet = TimeSpan.FromSeconds(60);
    protected static readonly TimeSpan Settle = TimeSpan.FromSeconds(30);
    protected static readonly DateTimeOffset Now = new(2026, 11, 10, 12, 0, 0, TimeSpan.Zero);

    private protected SubscriptionCheckSchedule _schedule;
    protected TimeSpan _next;

    /// <summary>
    /// Gets the number between 0 and 1 the schedule draws for spreading checks.
    /// </summary>
    protected virtual double Draw => 1;

    void Establish() => _schedule = new(Revise, SafetyNet, Settle, () => Draw);
}
