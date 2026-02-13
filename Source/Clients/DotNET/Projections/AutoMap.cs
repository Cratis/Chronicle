// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Projections;

/// <summary>
/// Represents the auto mapping behavior.
/// </summary>
public enum AutoMap
{
    /// <summary>
    /// Inherit the auto mapping behavior from the parent.
    /// </summary>
    Inherit = 0,

    /// <summary>
    /// Disable auto mapping.
    /// </summary>
    Disabled = 1,

    /// <summary>
    /// Enable auto mapping.
    /// </summary>
    Enabled = 2
}
