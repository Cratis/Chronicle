// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Contracts.Observation;

/// <summary>
/// Represents the state after an observation.
/// </summary>
public enum ObservationState
{
    /// <summary>
    /// Unknown state.
    /// </summary>
    None = 0,

    /// <summary>
    /// The observation was successful.
    /// </summary>
    Success = 1,

    /// <summary>
    /// The observation failed.
    /// </summary>
    Failed = 2,

    /// <summary>
    /// The observation was deferred: the partition is delivered again after a delay, without being failed.
    /// </summary>
    Deferred = 3
}
