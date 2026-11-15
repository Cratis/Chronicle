// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Projections.Engine.Graphs.Views;

/// <summary>
/// Represents how many related items a relation holds.
/// </summary>
public enum GraphViewCardinality
{
    /// <summary>
    /// A list of every related item, empty when there is none.
    /// </summary>
    Many = 0,

    /// <summary>
    /// The single related item, or nothing.
    /// </summary>
    One = 1
}
