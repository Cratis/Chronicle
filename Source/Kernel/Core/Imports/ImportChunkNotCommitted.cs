// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Imports;

namespace Cratis.Chronicle.Imports;

/// <summary>
/// The exception that is thrown when a chunk of an import could not be settled after every attempt.
/// </summary>
/// <param name="importId">The <see cref="ImportId"/>.</param>
/// <param name="chunkIndex">The index of the chunk.</param>
/// <param name="errors">Why the last attempt did not settle.</param>
public class ImportChunkNotCommitted(ImportId importId, int chunkIndex, IEnumerable<string> errors)
    : Exception($"Chunk {chunkIndex} of import '{importId}' could not be committed: {string.Join("; ", errors)}");
