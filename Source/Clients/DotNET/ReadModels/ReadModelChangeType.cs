// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.ReadModels;

/// <summary>
/// Represents the type of change that occurred to a read model instance.
/// </summary>
public enum ReadModelChangeType
{
    /// <summary>
    /// The read model instance was added (created for the first time).
    /// </summary>
    Added = 0,

    /// <summary>
    /// The read model instance was modified.
    /// </summary>
    Modified = 1,

    /// <summary>
    /// The read model instance was removed.
    /// </summary>
    Removed = 2
}
