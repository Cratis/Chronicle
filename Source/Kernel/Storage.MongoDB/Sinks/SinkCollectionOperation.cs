// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.MongoDB.Sinks;

/// <summary>
/// Defines the destructive operations on a read model's collections that are guarded.
/// </summary>
public enum SinkCollectionOperation
{
    /// <summary>
    /// Deleting every document in a collection.
    /// </summary>
    Clear = 0,

    /// <summary>
    /// Dropping a collection.
    /// </summary>
    Drop = 1,

    /// <summary>
    /// Renaming a collection.
    /// </summary>
    Rename = 2
}
