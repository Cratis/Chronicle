// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.MongoDB.Generations.Fencing;

/// <summary>
/// Defines what resolving a fenced batch with an unknown commit result found.
/// </summary>
public enum UnknownCommitResolution
{
    /// <summary>
    /// The batch's receipt exists; the batch committed and must not run again.
    /// </summary>
    Committed = 0,

    /// <summary>
    /// The gate was closed on the batch without its receipt and the epoch is still current; it can never commit and may run again.
    /// </summary>
    NotCommitted = 1,

    /// <summary>
    /// The gate was closed on the batch without its receipt and the epoch has moved on; the batch never committed and the owner is revoked.
    /// </summary>
    Revoked = 2
}
