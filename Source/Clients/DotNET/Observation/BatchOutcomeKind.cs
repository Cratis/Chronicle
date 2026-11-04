// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Observation;

/// <summary>
/// Describes what a batch method reported for a partition.
/// </summary>
internal enum BatchOutcomeKind
{
    /// <summary>
    /// The partition failed.
    /// </summary>
    Failed = 0,

    /// <summary>
    /// The partition is to be delivered again later, without failing.
    /// </summary>
    Deferred = 1
}
