// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Contracts.Projections;

/// <summary>
/// Represents the auto-mapping behavior for projections.
/// </summary>
public enum AutoMap
{
    /// <summary>
    /// Inherits the auto-map setting from parent context.
    /// </summary>
    Inherit = 0,

    /// <summary>
    /// Auto-mapping is disabled.
    /// </summary>
    Disabled = 1,

    /// <summary>
    /// Auto-mapping is enabled.
    /// </summary>
    Enabled = 2
}
