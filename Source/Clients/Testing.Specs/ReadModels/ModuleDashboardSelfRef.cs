// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Projections.ModelBound;
using Cratis.Chronicle.ReadModels;

namespace Cratis.Chronicle.Testing.ReadModels;

[Passive]
[FromEvent<ModuleCreatedSelfRef>]
public record ModuleDashboardSelfRef(
    ModuleNodeId Id,
    string Name,
    [ChildrenFrom<FeatureAddedSelfRef>(key: nameof(FeatureAddedSelfRef.FeatureId), identifiedBy: nameof(FeatureNodeSelfRef.Id), parentKey: nameof(FeatureAddedSelfRef.ModuleId))]
    IEnumerable<FeatureNodeSelfRef> Features);
