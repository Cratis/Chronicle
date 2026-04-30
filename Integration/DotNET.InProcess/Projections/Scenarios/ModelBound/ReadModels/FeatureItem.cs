// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.InProcess.Integration.Projections.Scenarios.ModelBound.Events;
using Cratis.Chronicle.Projections.ModelBound;

namespace Cratis.Chronicle.InProcess.Integration.Projections.Scenarios.ModelBound.ReadModels;

[FromEvent<FeatureAddedToModule>]
public record FeatureItem(
    Guid Id,
    string Name,
    [ChildrenFrom<SliceAddedToFeature>(
        key: nameof(SliceAddedToFeature.SliceId),
        parentKey: nameof(SliceAddedToFeature.FeatureId))]
    IEnumerable<SliceItem> Slices);
