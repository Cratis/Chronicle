// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Observation.Jobs;

/// <summary>
/// Represents the bounded set of observer job types the job gauges report.
/// </summary>
public enum ObserverJobGaugeType
{
    /// <summary>A job catching an observer up to the tail of its event sequence.</summary>
    CatchUp = 0,

    /// <summary>A job replaying the events of an observer.</summary>
    Replay = 1
}
