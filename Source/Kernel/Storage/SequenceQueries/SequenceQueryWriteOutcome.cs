// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.SequenceQueries;

/// <summary>
/// Describes what a conditional change to a saved query or folder did.
/// </summary>
public enum SequenceQueryWriteOutcome
{
    /// <summary>
    /// The change was applied, or there was nothing left to delete.
    /// </summary>
    Applied = 0,

    /// <summary>
    /// The change was not applied because the stored item differs from what the caller checked - it
    /// already exists, or it is stored under another owner now. Nothing was changed.
    /// </summary>
    Conflict = 1
}
