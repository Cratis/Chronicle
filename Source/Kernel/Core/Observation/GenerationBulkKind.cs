// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Observation;

/// <summary>
/// The kind of bulk window an observer can have open.
/// </summary>
internal enum GenerationBulkKind
{
    /// <summary>A replay window.</summary>
    Replay = 0,

    /// <summary>A catch-up window.</summary>
    CatchUp = 1
}
