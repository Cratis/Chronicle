// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Projections.ModelBound;
using Cratis.Chronicle.ReadModels;

namespace Cratis.Chronicle.Testing.ReadModels;

/// <summary>
/// Read model with a <c>[ChildrenFrom]</c> child collection whose children each carry a bulk
/// <see cref="IReadOnlyList{T}"/> of notes set whole from the child-creating event. Used to verify the
/// in-memory harness materializes a bulk list nested inside a keyed child (not just at the top level).
/// </summary>
/// <param name="Id">Order identifier.</param>
/// <param name="Reference">The order reference.</param>
/// <param name="Lines">The order lines keyed by <see cref="NotedLineAdded.LineNumber"/>.</param>
[Passive]
[FromEvent<NotedOrderOpened>]
public record NotedOrder(
    NotedOrderId Id,
    string Reference,

    [ChildrenFrom<NotedLineAdded>(key: nameof(NotedLineAdded.LineNumber))]
    IEnumerable<NotedLine> Lines);
