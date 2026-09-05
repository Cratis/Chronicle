// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Contracts.Observation;

/// <summary>
/// Defines the typed outcome for one observer in an <see cref="AppliedThroughResponse"/>.
/// </summary>
public enum AppliedThroughOutcome
{
    /// <summary>
    /// The outcome is unknown. Never a produced value - a closed sentinel guarding against an unset field.
    /// </summary>
    Unknown = 0,

    /// <summary>
    /// The observer durably applied through the target position.
    /// </summary>
    Ready = 1,

    /// <summary>
    /// The observer had not reached the target position before the caller's deadline.
    /// </summary>
    TimedOut = 2,

    /// <summary>
    /// The observer id named in the request does not exist for the event sequence.
    /// </summary>
    Unavailable = 3,

    /// <summary>
    /// The observer has a failed partition and cannot make further progress without intervention.
    /// </summary>
    Failed = 4,

    /// <summary>
    /// The observer is replaying and its position cannot be trusted as forward progress.
    /// </summary>
    Replaying = 5,

    /// <summary>
    /// The observer is quarantined and will not resume on its own.
    /// </summary>
    Quarantined = 6,
}
