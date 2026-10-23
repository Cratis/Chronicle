// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Keys;
using Cratis.Chronicle.Projections.ModelBound;

namespace Cratis.Chronicle.Benchmarks.Clustering;

/// <summary>
/// A synthetic root with a hierarchy of separately sourced children.
/// </summary>
/// <param name="Id">The root identity.</param>
/// <param name="Name">The root name.</param>
/// <param name="Children">The first level of children.</param>
[FromEvent<ReplayShapeRootCreated>]
public record ReplayShapeReadModel(
    [Key] string Id,
    string Name,
    [ChildrenFrom<ReplayShapeChildAdded>(key: nameof(ReplayShapeChildAdded.ChildId), parentKey: nameof(ReplayShapeChildAdded.RootId), identifiedBy: nameof(ReplayShapeChild.Id))]
    IEnumerable<ReplayShapeChild> Children);
