// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Projections.ModelBound;

namespace Cratis.Chronicle.Integration.Clustering.for_Clustering.Hierarchy;

[FromEvent<HierarchyModuleAdded>]
public record HierarchyModule(
    Guid Id,
    string Name,
    [ChildrenFrom<HierarchyFeatureAdded>(
        key: nameof(HierarchyFeatureAdded.FeatureId),
        parentKey: nameof(HierarchyFeatureAdded.ModuleId))]
    IEnumerable<HierarchyFeature> Features);
