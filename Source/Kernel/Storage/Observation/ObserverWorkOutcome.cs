// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.Observation;

/// <summary>
/// A durable step checkpoint or terminal execution outcome.
/// </summary>
public enum ObserverWorkOutcome
{
    /// <summary>
    /// A durable checkpoint, not a terminal outcome.
    /// </summary>
    Checkpoint = 0,

    /// <summary>
    /// All work succeeded.
    /// </summary>
    Success = 1,

    /// <summary>
    /// Work stopped with durable partial progress.
    /// </summary>
    Stopped = 2,

    /// <summary>
    /// Work failed with durable partial progress.
    /// </summary>
    Failed = 3,

    /// <summary>
    /// Work is held awaiting a handler.
    /// </summary>
    Held = 4
}
