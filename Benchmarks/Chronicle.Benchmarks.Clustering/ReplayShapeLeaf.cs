// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Keys;
using Cratis.Chronicle.Projections.ModelBound;

namespace Cratis.Chronicle.Benchmarks.Clustering;

/// <summary>
/// A second-level child with no further descendants.
/// </summary>
/// <param name="Id">The leaf identity.</param>
/// <param name="Name">The last name written.</param>
[FromEvent<ReplayShapeDescendantAdded>(key: nameof(ReplayShapeDescendantAdded.ChildId), parentKey: nameof(ReplayShapeDescendantAdded.ParentId))]
[FromEvent<ReplayShapeDescendantRenamed>(key: nameof(ReplayShapeDescendantRenamed.ChildId), parentKey: nameof(ReplayShapeDescendantRenamed.ParentId))]
[RemovedWith<ReplayShapeDescendantRemoved>(key: nameof(ReplayShapeDescendantRemoved.ChildId), parentKey: nameof(ReplayShapeDescendantRemoved.ParentId))]
public record ReplayShapeLeaf(
    [Key] string Id,
    [SetFrom<ReplayShapeDescendantRenamed>(nameof(ReplayShapeDescendantRenamed.NewName))] string Name);
