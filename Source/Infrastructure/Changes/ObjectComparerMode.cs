// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Changes;

/// <summary>
/// Defines the mode to use when comparing objects.
/// </summary>
public enum ObjectComparerMode
{
    /// <summary>
    /// Strict mode. Collections and dictionaries are compared in order.
    /// </summary>
    Strict = 0,

    /// <summary>
    /// Loose mode. Collections and dictionaries are compared without regard to order.
    /// As long as the same items exist in both, they are considered equal.
    /// </summary>
    Loose = 1
}
