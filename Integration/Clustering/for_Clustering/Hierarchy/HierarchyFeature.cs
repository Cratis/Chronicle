// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Projections.ModelBound;

namespace Cratis.Chronicle.Integration.Clustering.for_Clustering.Hierarchy;

[FromEvent<HierarchyFeatureAdded>]
public record HierarchyFeature(
    Guid Id,
    string Name,
    [ChildrenFrom<HierarchySliceAdded>(
        key: nameof(HierarchySliceAdded.SliceId),
        parentKey: nameof(HierarchySliceAdded.FeatureId))]
    IEnumerable<HierarchySlice> Slices);
