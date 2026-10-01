// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Projections.ModelBound;

namespace Cratis.Chronicle.Integration.Clustering.for_Clustering.Hierarchy;

[FromEvent<HierarchySliceAdded>]
public record HierarchySlice(
    Guid Id,
    string Name,
    [ChildrenFrom<HierarchyEventAdded>(
        key: nameof(HierarchyEventAdded.EventItemId),
        parentKey: nameof(HierarchyEventAdded.SliceId))]
    IEnumerable<HierarchyEventItem> Events);
