// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Events.Constraints;

/// <summary>
/// Defines how unique property values are retained.
/// </summary>
public enum UniqueConstraintMode
{
    /// <summary>
    /// Keep one value per event source, releasing its previous value on replacement.
    /// </summary>
    PerEventSource = 0,

    /// <summary>
    /// Keep every value until a removal event releases it.
    /// </summary>
    PerValue = 1
}
