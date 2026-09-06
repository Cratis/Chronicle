// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Compatibility;

/// <summary>
/// Represents how many values a field carries on the wire.
/// </summary>
public enum WireFieldLabel
{
    /// <summary>A single value.</summary>
    Singular = 0,

    /// <summary>Zero or more values.</summary>
    Repeated = 1
}
