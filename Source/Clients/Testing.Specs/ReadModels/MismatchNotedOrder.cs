// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Projections.ModelBound;
using Cratis.Chronicle.ReadModels;

namespace Cratis.Chronicle.Testing.ReadModels;

/// <summary>
/// Read model that deliberately reproduces the silent AutoMap-to-nothing bug: the child's
/// <see cref="MismatchNotedLine.Annotations"/> list has no same-named property on <see cref="NotedLineAdded"/>
/// and no explicit mapping, so it always projects empty. CHR0033 (compile-time) and the projection-factory
/// warning (runtime) now flag exactly this shape — suppressed here because the mismatch is intentional.
/// </summary>
/// <param name="Id">Order identifier.</param>
/// <param name="Reference">The order reference.</param>
/// <param name="Lines">The order lines keyed by <see cref="NotedLineAdded.LineNumber"/>.</param>
#pragma warning disable CHR0033
[Passive]
[FromEvent<NotedOrderOpened>]
public record MismatchNotedOrder(
    NotedOrderId Id,
    string Reference,

    [ChildrenFrom<NotedLineAdded>(key: nameof(NotedLineAdded.LineNumber))]
    IEnumerable<MismatchNotedLine> Lines);
#pragma warning restore CHR0033
