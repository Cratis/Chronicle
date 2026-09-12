// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Observation;

/// <summary>
/// The minimum safe operation Chronicle determined for a changed read-model observer definition.
/// </summary>
internal enum DefinitionEvolutionOperation
{
    /// <summary>
    /// Existing read models cannot be affected.
    /// </summary>
    NoAction = 0,

    /// <summary>
    /// Only event-source partitions containing the affected event types need rebuilding.
    /// </summary>
    PartialReplay = 1,

    /// <summary>
    /// The complete observer must be rebuilt.
    /// </summary>
    FullReplay = 2
}
