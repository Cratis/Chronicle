// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Keys;
using Cratis.Chronicle.Projections.ModelBound;

namespace Cratis.Chronicle.Benchmarks.Clustering;

/// <summary>
/// A first-level child whose leaf children are appended on their own event sources.
/// </summary>
/// <param name="Id">The child identity.</param>
/// <param name="Name">The last name written.</param>
/// <param name="Leaves">Nested children.</param>
[FromEvent<ReplayShapeChildAdded>]
[FromEvent<ReplayShapeChildRenamed>(key: nameof(ReplayShapeChildRenamed.ChildId), parentKey: nameof(ReplayShapeChildRenamed.ParentId))]
[RemovedWith<ReplayShapeChildRemoved>(key: nameof(ReplayShapeChildRemoved.ChildId), parentKey: nameof(ReplayShapeChildRemoved.ParentId))]
public record ReplayShapeChild(
    [Key] string Id,
    [SetFrom<ReplayShapeChildRenamed>(nameof(ReplayShapeChildRenamed.NewName))] string Name,
    [ChildrenFrom<ReplayShapeDescendantAdded>(key: nameof(ReplayShapeDescendantAdded.ChildId), parentKey: nameof(ReplayShapeDescendantAdded.ParentId), identifiedBy: nameof(ReplayShapeLeaf.Id))]
    IEnumerable<ReplayShapeLeaf> Leaves);
