// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Imports;

/// <summary>
/// The exception that is thrown when the kernel refuses a chunk of an import as a whole.
/// </summary>
/// <param name="chunkIndex">The index of the chunk.</param>
/// <param name="reason">Why.</param>
public class ImportChunkRefused(int chunkIndex, string reason) : Exception($"Chunk {chunkIndex} of the import was refused: {reason}");
