// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Contracts.Projections;

/// <summary>
/// Defines the direction of a sort.
/// </summary>
public enum SortDirection
{
    /// <summary>
    /// Lowest value first. Missing and null values come first.
    /// </summary>
    Ascending = 0,

    /// <summary>
    /// Highest value first. Missing and null values come last.
    /// </summary>
    Descending = 1
}
