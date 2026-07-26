// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;

namespace Cratis.Chronicle.Benchmarks.Clustering;

/// <summary>
/// Event the fixture drives through the full pipeline to confirm the cluster is operational before any
/// measured window opens.
/// </summary>
/// <param name="Value">An arbitrary value.</param>
[EventType]
public record ClusterWarmedUp(int Value);
