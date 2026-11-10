// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Imports;

namespace Cratis.Chronicle.Imports;

/// <summary>
/// The exception that is thrown when a chunk an import counted is no longer in staging.
/// </summary>
/// <param name="importId">The <see cref="ImportId"/>.</param>
/// <param name="chunkIndex">The index of the missing chunk.</param>
public class ImportChunkMissing(ImportId importId, int chunkIndex) : Exception($"Chunk {chunkIndex} of import '{importId}' is not in staging.");
