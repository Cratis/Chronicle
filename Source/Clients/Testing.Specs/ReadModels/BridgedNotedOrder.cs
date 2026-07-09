// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Projections.ModelBound;
using Cratis.Chronicle.ReadModels;

namespace Cratis.Chronicle.Testing.ReadModels;

/// <summary>
/// Read model whose child collection is named differently from the event list, but bridged with
/// <c>[SetFrom]</c> on <see cref="BridgedNotedLine.Annotations"/> so it projects correctly — proving a rename
/// is not the only fix for the AutoMap-to-nothing failure.
/// </summary>
/// <param name="Id">Order identifier.</param>
/// <param name="Reference">The order reference.</param>
/// <param name="Lines">The order lines keyed by <see cref="NotedLineAdded.LineNumber"/>.</param>
[Passive]
[FromEvent<NotedOrderOpened>]
public record BridgedNotedOrder(
    NotedOrderId Id,
    string Reference,

    [ChildrenFrom<NotedLineAdded>(key: nameof(NotedLineAdded.LineNumber))]
    IEnumerable<BridgedNotedLine> Lines);
