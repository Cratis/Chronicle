// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.MongoDB;

/// <summary>
/// The outcome of moving a database.
/// </summary>
internal enum DatabaseMoveOutcome
{
    /// <summary>
    /// The source had no collections, so there was nothing to move.
    /// </summary>
    NothingToMove = 0,

    /// <summary>
    /// Everything was copied, the copy was confirmed, and the source was dropped.
    /// </summary>
    Moved = 1,

    /// <summary>
    /// The copy could not be confirmed, so the source was kept.
    /// </summary>
    Kept = 2
}
