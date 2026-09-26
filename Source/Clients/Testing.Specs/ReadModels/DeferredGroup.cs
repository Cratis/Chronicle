// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Keys;
using Cratis.Chronicle.Projections.ModelBound;

namespace Cratis.Chronicle.Testing.ReadModels;

public record DeferredGroup(
    [Key] Guid Id,
    string Name,
    [ChildrenFrom<DeferredItemAdded>(key: nameof(DeferredItemAdded.ItemId), identifiedBy: nameof(DeferredItem.Id), parentKey: nameof(DeferredItemAdded.GroupId))]
    IEnumerable<DeferredItem> Items,
    [ChildrenFrom<DeferredSectionAdded>(key: nameof(DeferredSectionAdded.SectionId), identifiedBy: nameof(DeferredSection.Id), parentKey: nameof(DeferredSectionAdded.GroupId))]
    IEnumerable<DeferredSection> Sections);
