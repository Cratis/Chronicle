// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Benchmarks.Clustering;

/// <summary>
/// Read model built by the <see cref="ProjectionThroughputProjection"/>.
/// </summary>
/// <param name="Name">The last observed event name.</param>
/// <param name="Value">The last observed value.</param>
public record ProjectionThroughputReadModel(string Name, int Value);
