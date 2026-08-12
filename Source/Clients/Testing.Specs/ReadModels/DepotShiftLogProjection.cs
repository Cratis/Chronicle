// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Projections;

namespace Cratis.Chronicle.Testing.ReadModels;

/// <summary>
/// Fluent projection for <see cref="DepotShiftLog"/> where a single event type — <see cref="ShiftLogged"/> —
/// is both a root <c>.From&lt;T&gt;</c> source mapping a parent scalar and a child <c>.From&lt;T&gt;</c> source
/// feeding the child collection. The root then owns the key resolver for that event type, so the child's own
/// (indexed) resolver is never folded into the root's map — the shape that exposes whether the harness resolves
/// a child key of its own before handing the event to the child projection.
/// </summary>
public class DepotShiftLogProjection : IProjectionFor<DepotShiftLog>
{
    /// <inheritdoc/>
    public void Define(IProjectionBuilderFor<DepotShiftLog> builder) => builder
        .From<ShiftLogged>(b => b.Set(m => m.Depot).To(e => e.Depot))
        .Children(_ => _.Shifts, shifts => shifts
            .IdentifiedBy(_ => _.Worker)
            .From<ShiftLogged>(from => from.UsingKey(e => e.Worker)));
}
