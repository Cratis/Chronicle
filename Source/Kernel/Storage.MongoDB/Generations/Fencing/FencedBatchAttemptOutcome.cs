// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.MongoDB.Generations.Fencing;

/// <summary>
/// Defines how one attempt of a fenced batch ended.
/// </summary>
internal enum FencedBatchAttemptOutcome
{
    /// <summary>
    /// The attempt committed.
    /// </summary>
    Committed = 0,

    /// <summary>
    /// The epoch has moved on; the attempt did not commit and no attempt may.
    /// </summary>
    Revoked = 1,

    /// <summary>
    /// The attempt did not commit and may be made again.
    /// </summary>
    Retry = 2
}
