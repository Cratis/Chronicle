// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.Observation.Delivery;

/// <summary>
/// Identifies why a key is parked. Unknown numeric values must be retained.
/// </summary>
public enum ParkedKeyKind
{
    /// <summary>
    /// The reason is unknown.
    /// </summary>
    Unknown = 0,

    /// <summary>
    /// Event handling failed.
    /// </summary>
    Handling = 1,

    /// <summary>
    /// Event handling timed out.
    /// </summary>
    Timeout = 2,

    /// <summary>
    /// The observer disconnected.
    /// </summary>
    Disconnected = 3,

    /// <summary>
    /// Replay parked the key.
    /// </summary>
    Replay = 4
}
