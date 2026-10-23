// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Benchmarks.Clustering;

/// <summary>
/// One measured configuration of the fence cost scenario.
/// </summary>
/// <param name="Members">The number of replica-set members.</param>
/// <param name="Fenced">Whether the writes are fenced.</param>
/// <param name="BatchSize">The number of upserts per batch.</param>
/// <param name="Batches">The number of measured batches per writer.</param>
/// <param name="ChangeStreams">The number of change streams open on the collection.</param>
/// <param name="Writers">The number of parallel writers, each with its own materialization key.</param>
internal sealed record FenceCostConfiguration(int Members, bool Fenced, int BatchSize, int Batches, int ChangeStreams, int Writers);
