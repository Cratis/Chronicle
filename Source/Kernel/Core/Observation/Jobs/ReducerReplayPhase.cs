// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Observation.Jobs;

/// <summary>
/// Durable publication state; job step counts alone do not prove publication.
/// </summary>
public enum ReducerReplayPhase
{
    /// <summary>
    /// No successful rebuild has been recorded.
    /// </summary>
    Building = 0,

    /// <summary>
    /// Every step succeeded and publication may be in progress or committed with a lost reply.
    /// </summary>
    Publishing = 1,

    /// <summary>
    /// Storage acknowledged publication; completion uses the rebuilt watermark.
    /// </summary>
    Published = 2,

    /// <summary>
    /// The attempt cannot publish; the old model and position remain authoritative.
    /// </summary>
    Abandoned = 3
}
