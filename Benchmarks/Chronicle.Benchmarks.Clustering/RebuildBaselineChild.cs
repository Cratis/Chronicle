// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Keys;
using Cratis.Chronicle.Projections.ModelBound;

namespace Cratis.Chronicle.Benchmarks.Clustering;

/// <summary>A separately sourced child owned only by the rebuild baseline.</summary>
/// <param name="Id">The child identity.</param>
/// <param name="Name">The child value.</param>
[FromEvent<RebuildBaselineChildAdded>]
public record RebuildBaselineChild([Key] string Id, string Name);
