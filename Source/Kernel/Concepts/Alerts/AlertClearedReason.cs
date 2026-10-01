// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Concepts.Alerts;

/// <summary>
/// Represents why an alert incident ended.
/// </summary>
public enum AlertClearedReason
{
    /// <summary>
    /// The partition succeeded again by itself.
    /// </summary>
    Recovered = 0,

    /// <summary>
    /// A person or an operation cleared the failed partitions or the observer quarantine.
    /// </summary>
    Cleared = 1,

    /// <summary>
    /// A fresh client subscription revived the observer from quarantine.
    /// </summary>
    Revived = 2,

    /// <summary>
    /// The observer was removed.
    /// </summary>
    Removed = 3
}
