// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Chaos;

/// <summary>
/// Materialized projection count and sum for one event source.
/// </summary>
/// <param name="Id">The source identity.</param>
/// <param name="Count">Number of contributions.</param>
/// <param name="Sum">Total contribution.</param>
public sealed record ChaosProjectionModel(string Id, int Count, long Sum);
