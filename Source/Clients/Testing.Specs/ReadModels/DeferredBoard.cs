// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Projections.ModelBound;
using Cratis.Chronicle.ReadModels;

namespace Cratis.Chronicle.Testing.ReadModels;

[Passive]
[FromEvent<DeferredBoardOpened>]
[FromEvent<DeferredItemAdded>(key: nameof(DeferredItemAdded.GroupId))]
[FromEvent<DeferredSectionAdded>(key: nameof(DeferredSectionAdded.GroupId))]
[FromEvent<DeferredSectionItemAdded>(key: nameof(DeferredSectionItemAdded.GroupId))]
public record DeferredBoard(
    Guid Id,
    [AddFrom<DeferredItemAdded>(nameof(DeferredItemAdded.Amount))] int TotalAmount,
    [AddFrom<DeferredSectionAdded>(nameof(DeferredSectionAdded.Amount))] int SectionAmount,
    [AddFrom<DeferredSectionItemAdded>(nameof(DeferredSectionItemAdded.Amount))] int NestedItemAmount,
    [ChildrenFrom<DeferredGroupAdded>(key: nameof(DeferredGroupAdded.GroupId), identifiedBy: nameof(DeferredGroup.Id))]
    IEnumerable<DeferredGroup> Groups);
