// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Keys;
using Cratis.Chronicle.Projections.ModelBound;

namespace Cratis.Chronicle.Testing.ReadModels;

public record DeferredSection(
    [Key] Guid Id,
    string Name,
    [ChildrenFrom<DeferredSectionItemAdded>(key: nameof(DeferredSectionItemAdded.ItemId), identifiedBy: nameof(DeferredItem.Id), parentKey: nameof(DeferredSectionItemAdded.SectionId))]
    IEnumerable<DeferredItem> Items);
