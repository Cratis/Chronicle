// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.MongoDB.Integration.Generations.for_ReleasedKernelGenerationFloor;

/// <summary>
/// Serializes the released-kernel floor specs: each starts containers that reserve fixed kernel ports.
/// </summary>
[CollectionDefinition(Name, DisableParallelization = true)]
public static class ReleasedFloorCollection
{
    /// <summary>The collection name.</summary>
    public const string Name = "Released kernel generation floor";
}
