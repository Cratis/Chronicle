// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Benchmarks.Clustering;

/// <summary>
/// Read model produced by the <see cref="WarmupReducer"/>.
/// </summary>
/// <param name="Value">The warmed-up value.</param>
public record WarmupReadModel(int Value);
