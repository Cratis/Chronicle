// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Keys;
using Cratis.Chronicle.Projections.ModelBound;

namespace Cratis.Chronicle.Benchmarks.Clustering;

/// <summary>One baseline root with an unordered, separately sourced child collection.</summary>
/// <param name="Id">The root key.</param>
/// <param name="CatalogId">The joined catalog key.</param>
/// <param name="Revision">The last root revision.</param>
/// <param name="Category">The joined value.</param>
/// <param name="Children">Children identified by their own keys.</param>
[FromEvent<RebuildBaselineValueChanged>]
[RemovedWith<RebuildBaselineRemoved>]
public record RebuildBaselineModel(
    [Key] string Id,
    string CatalogId,
    int Revision,
    [Join<RebuildBaselineCatalogNamed>(on: nameof(CatalogId))] string Category,
    [ChildrenFrom<RebuildBaselineChildAdded>(key: nameof(RebuildBaselineChildAdded.ChildId), parentKey: nameof(RebuildBaselineChildAdded.RootId), identifiedBy: nameof(RebuildBaselineChild.Id))]
    IEnumerable<RebuildBaselineChild> Children);
