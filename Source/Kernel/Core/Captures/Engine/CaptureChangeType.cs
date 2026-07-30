// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Captures.Engine;

/// <summary>
/// Represents the kind of change observed for an item between two capture cycles.
/// </summary>
public enum CaptureChangeType
{
    /// <summary>
    /// The item was not present in the previous observation.
    /// </summary>
    Added = 0,

    /// <summary>
    /// The item was present in the previous observation but is gone from the current one.
    /// </summary>
    Removed = 1,

    /// <summary>
    /// The item is present in both observations with different content.
    /// </summary>
    Modified = 2
}
