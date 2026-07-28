// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;

namespace Cratis.Chronicle.Benchmarks.Clustering;

/// <summary>
/// Event observed by the <see cref="ReplayThroughputProjection"/>, seeded before the measured window and
/// reprocessed by the replay the benchmark measures.
/// </summary>
/// <param name="Name">The event name.</param>
/// <param name="Value">The benchmark value.</param>
[EventType]
public record ReplayedEvent(string Name, int Value);
