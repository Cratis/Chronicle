// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Keys;
using Cratis.Chronicle.Projections.ModelBound;

namespace Cratis.Chronicle.Testing.ReadModels;

[FromEvent<FeatureAddedSelfRef>]
[FromEvent<SubFeatureAddedSelfRef>(parentKey: nameof(SubFeatureAddedSelfRef.ParentFeatureId))]
public record FeatureNodeSelfRef(
    [Key] FeatureNodeId Id,
    string Name,
    [ChildrenFrom<SubFeatureAddedSelfRef>(identifiedBy: nameof(FeatureNodeSelfRef.Id), parentKey: nameof(SubFeatureAddedSelfRef.ParentFeatureId))]
    IEnumerable<FeatureNodeSelfRef> SubFeatures);
