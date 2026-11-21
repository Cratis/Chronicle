// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Benchmarks.Storage;

/// <summary>The workload a ledger describes.</summary>
/// <param name="Shape">The sizes measured.</param>
/// <param name="PayloadBytes">The event payload size.</param>
/// <param name="Client">The client settings.</param>
public sealed record WorkloadSetup(WorkloadShape Shape, int PayloadBytes, ClientSettings Client);
