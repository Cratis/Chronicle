// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.Imports;

/// <summary>
/// Represents what became of a chunk handed to <see cref="IImportStorage.AddChunk"/>.
/// </summary>
public enum StagedChunkAddition
{
    /// <summary>The chunk was stored.</summary>
    Added = 0,

    /// <summary>The same chunk was already staged under its index and was kept.</summary>
    AlreadyStaged = 1,

    /// <summary>Another chunk, covering other events, is staged under the same index; nothing was stored.</summary>
    Conflicting = 2
}
