// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle;

#pragma warning disable CA1700 // The names describe the rule, not an unused placeholder.

/// <summary>
/// Describes which part of the resource name rule a name breaks.
/// </summary>
public enum ResourceNameViolation
{
    /// <summary>
    /// The name is acceptable.
    /// </summary>
    None = 0,

    /// <summary>
    /// The name is null, empty or only whitespace.
    /// </summary>
    Empty = 1,

    /// <summary>
    /// The name is the not set marker.
    /// </summary>
    NotSet = 2,

    /// <summary>
    /// The name contains a reserved character.
    /// </summary>
    ReservedCharacter = 3,

    /// <summary>
    /// The name is a reserved name.
    /// </summary>
    ReservedName = 4,

    /// <summary>
    /// The combination of event store name and namespace name is reserved.
    /// </summary>
    ReservedCombination = 5
}
#pragma warning restore CA1700
