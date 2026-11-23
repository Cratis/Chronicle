// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Integration.Projections.Scenarios.ModelBound.Events;
using Cratis.Chronicle.Integration.Projections.Scenarios.ModelBound.ReadModels;
using Cratis.Chronicle.Keys;
using Cratis.Chronicle.Projections.ModelBound;

namespace Cratis.Chronicle.Integration.Projections.Scenarios.ModelBound.when_projecting_deep_self_referential_children;

[FromEvent<FeatureAddedToModule>]
[FromEvent<SubFeatureAdded>(parentKey: nameof(SubFeatureAdded.ParentFeatureId))]
public record RecursiveFeature(
    [Key] Guid Id,
    string Name,
    [ChildrenFrom<SubFeatureAdded>(parentKey: nameof(SubFeatureAdded.ParentFeatureId), identifiedBy: nameof(Id))]
    IEnumerable<RecursiveFeature> SubFeatures,
    [ChildrenFrom<SliceAddedToFeature>(key: nameof(SliceAddedToFeature.SliceId), parentKey: nameof(SliceAddedToFeature.FeatureId))]
    IEnumerable<SliceItem> Slices);
