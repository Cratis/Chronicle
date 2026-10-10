// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.EventSequences;

/// <summary>
/// Describes the read-time outcome of caused-by identity resolution.
/// </summary>
public enum IdentityResolution
{
    /// <summary>
    /// The identity exists and has a display name.
    /// </summary>
    Resolved = 0,

    /// <summary>
    /// The identity exists, but its display name is empty.
    /// </summary>
    NameUnavailable = 1,

    /// <summary>
    /// The identity is absent from storage.
    /// </summary>
    Missing = 2,

    /// <summary>
    /// No identity was set.
    /// </summary>
    NotSet = 3,

    /// <summary>
    /// Chronicle caused the event.
    /// </summary>
    System = 4
}
