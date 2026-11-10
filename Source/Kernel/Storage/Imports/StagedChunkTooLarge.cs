// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Imports;

namespace Cratis.Chronicle.Storage.Imports;

/// <summary>
/// The exception that is thrown when a staged chunk is larger than the storage provider can hold in one record.
/// </summary>
/// <param name="importId">The <see cref="ImportId"/>.</param>
/// <param name="chunkIndex">The chunk index.</param>
/// <param name="size">The serialized size of the chunk, in bytes.</param>
/// <param name="limit">The most the provider holds, in bytes.</param>
public class StagedChunkTooLarge(ImportId importId, int chunkIndex, long size, long limit)
    : Exception($"Chunk {chunkIndex} of import '{importId}' is {size} bytes when stored; the storage holds at most {limit} bytes in one chunk. Send smaller chunks.");
