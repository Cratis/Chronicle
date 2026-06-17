// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Integration.Projections.Scenarios.ModelBound.Events;
using Cratis.Chronicle.Projections.ModelBound;

namespace Cratis.Chronicle.Integration.Projections.Scenarios.ModelBound.ReadModels;

[FromEvent<RegionDefined>]
public record RegionReadModel(
    Guid Id,
    string Name,
    [ChildrenFrom<MarkerPlacedInRegion>(
        key: nameof(MarkerPlacedInRegion.MarkerId),
        parentKey: nameof(MarkerPlacedInRegion.RegionId))]
    IEnumerable<RegionMarkerItem> Markers);
