// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.Imports;

/// <summary>
/// Represents a contiguous range of sequence numbers one committed import chunk was given.
/// </summary>
/// <param name="ChunkIndex">The staged chunk the range was committed from.</param>
/// <param name="First">The first sequence number.</param>
/// <param name="Last">The last sequence number.</param>
public record ImportCommittedRange(int ChunkIndex, ulong First, ulong Last);
