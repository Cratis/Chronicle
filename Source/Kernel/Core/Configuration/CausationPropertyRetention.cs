// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Configuration;

/// <summary>
/// Specifies whether causation property values are persisted with events.
/// </summary>
public enum CausationPropertyRetention
{
    /// <summary>
    /// Persists causation properties unchanged.
    /// </summary>
    Retain = 0,

    /// <summary>
    /// Persists the causation type and time, but omits its properties.
    /// </summary>
    Omit = 1
}
