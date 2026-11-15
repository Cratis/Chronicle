// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Graphs;

/// <summary>
/// Defines how many related items a relation of a view holds.
/// </summary>
public enum GraphViewCardinality
{
    /// <summary>
    /// Every related item, as a list. No related item gives an empty list.
    /// </summary>
    Many = 0,

    /// <summary>
    /// One related item. No related item gives null.
    /// </summary>
    One = 1
}
