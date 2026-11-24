// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Diagnostics.Load;

/// <summary>
/// What a silo is busy with.
/// </summary>
/// <param name="Status">The <see cref="KernelLoadStatus"/> of the silo.</param>
/// <param name="Reasons">Why the silo is degraded, one entry per limit crossed. Empty when it is not.</param>
/// <param name="Grains">The grain load per grain type and event store, busiest first.</param>
/// <param name="OpenStreams">How many gRPC streaming calls the silo is serving.</param>
public record KernelLoadReport(KernelLoadStatus Status, IReadOnlyList<string> Reasons, IReadOnlyList<GrainLoad> Grains, int OpenStreams);
