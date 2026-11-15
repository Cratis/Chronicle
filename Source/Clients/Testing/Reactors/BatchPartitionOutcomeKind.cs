// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Testing.Reactors;

/// <summary>
/// The outcomes a batch method can report for a single partition.
/// </summary>
public enum BatchPartitionOutcomeKind
{
    /// <summary>
    /// The batch method failed the partition with <c language="csharp">batch.Fail(...)</c>.
    /// </summary>
    Failed = 0,

    /// <summary>
    /// The batch method deferred the partition with <c language="csharp">batch.Defer(...)</c>.
    /// </summary>
    Deferred = 1
}
