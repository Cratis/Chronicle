// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Keys;
using Cratis.Chronicle.Projections.ModelBound;

namespace Cratis.Chronicle.Testing.ReadModels;

[FromEvent<FeatureAddedSelfRef>]
[FromEvent<SubFeatureAddedSelfRef>(parentKey: nameof(SubFeatureAddedSelfRef.ParentFeatureId))]
public record FeatureWithSlicesSelfRef(
    [Key] FeatureNodeId Id,
    string Name,
    [ChildrenFrom<SubFeatureAddedSelfRef>(identifiedBy: nameof(FeatureWithSlicesSelfRef.Id), parentKey: nameof(SubFeatureAddedSelfRef.ParentFeatureId))]
    IReadOnlyList<FeatureWithSlicesSelfRef> SubFeatures,
    [ChildrenFrom<StateChangeSliceAddedSelfRef>(key: nameof(StateChangeSliceAddedSelfRef.SliceId), identifiedBy: nameof(SliceNodeSelfRef.Id), parentKey: nameof(StateChangeSliceAddedSelfRef.FeatureId))]
    IReadOnlyList<SliceNodeSelfRef> Slices);
