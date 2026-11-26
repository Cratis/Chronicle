// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Testing.Assertions;

/// <summary>
/// Defines the property and runtime-type contract of a comparison.
/// </summary>
internal enum ValueComparison
{
    /// <summary>
    /// Compare only listed expected properties.
    /// </summary>
    Partial = 0,

    /// <summary>
    /// Compare complete public property sets without requiring object-shape types to match.
    /// </summary>
    Exact = 1,

    /// <summary>
    /// Compare complete public property sets and exact runtime types.
    /// </summary>
    Typed = 2
}
