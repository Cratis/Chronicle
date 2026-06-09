// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Integration.Clustering;

/// <summary>
/// Read model produced by the warmup reducer.
/// </summary>
/// <param name="Value">The warmed-up value.</param>
public record ClusterWarmupReadModel(int Value);
