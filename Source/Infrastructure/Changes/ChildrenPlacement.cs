// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Changes;

/// <summary>
/// Defines where a new child is placed in a children collection.
/// </summary>
public enum ChildrenPlacement
{
    /// <summary>
    /// A new child is added to the end of the collection. This is the default.
    /// </summary>
    Append = 0,

    /// <summary>
    /// A new child is inserted at the start of the collection.
    /// </summary>
    Prepend = 1,

    /// <summary>
    /// The collection is ordered by a child property after a new child is inserted.
    /// </summary>
    Sorted = 2
}
