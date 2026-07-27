// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;

namespace Cratis.Chronicle.Benchmarks.Clustering;

/// <summary>
/// Event observed by the <see cref="ProjectionThroughputProjection"/>.
/// </summary>
/// <param name="Name">The event name.</param>
/// <param name="Value">The benchmark value.</param>
[EventType]
public record ProjectedEvent(string Name, int Value);
