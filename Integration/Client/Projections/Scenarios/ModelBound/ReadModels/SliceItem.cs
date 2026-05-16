// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Integration.Projections.Scenarios.ModelBound.Events;
using Cratis.Chronicle.Projections.ModelBound;

namespace Cratis.Chronicle.Integration.Projections.Scenarios.ModelBound.ReadModels;

[FromEvent<SliceAddedToFeature>]
public record SliceItem(
    Guid Id,
    string Name,
    [ChildrenFrom<EventAddedToSlice>(
        key: nameof(EventAddedToSlice.EventItemId),
        parentKey: nameof(EventAddedToSlice.SliceId))]
    IEnumerable<EventItem> Events,
    [Nested] SliceCommandItem? Command);
