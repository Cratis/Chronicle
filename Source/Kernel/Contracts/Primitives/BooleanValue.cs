// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Contracts.Primitives;

/// <summary>
/// Represents an explicit boolean value independently of proto3 scalar defaults.
/// </summary>
public enum BooleanValue
{
    /// <summary>
    /// No explicit value was supplied; use the legacy boolean field.
    /// </summary>
    Unspecified = 0,

    /// <summary>
    /// The explicitly supplied value is true.
    /// </summary>
    True = 1,

    /// <summary>
    /// The explicitly supplied value is false.
    /// </summary>
    False = 2
}
