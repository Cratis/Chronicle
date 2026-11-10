// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Imports;

/// <summary>
/// Represents a range of sequence numbers one chunk of an import was committed at.
/// </summary>
/// <param name="ChunkIndex">The staged chunk.</param>
/// <param name="First">The first sequence number.</param>
/// <param name="Last">The last sequence number.</param>
public record ImportSequenceRange(int ChunkIndex, ulong First, ulong Last);
