// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Projections;

namespace Cratis.Chronicle.Testing.ReadModels;

/// <summary>
/// Fluent projection for <see cref="DepotShiftBoard"/> using the <c>AddChild(...)</c> spelling inside the root
/// <c>.From&lt;ShiftLogged&gt;</c> — which desugars into <c>Children(...).From&lt;ShiftLogged&gt;</c> and so
/// produces the same one-event-does-both shape as <see cref="DepotShiftLogProjection"/>.
/// </summary>
public class DepotShiftBoardProjection : IProjectionFor<DepotShiftBoard>
{
    /// <inheritdoc/>
    public void Define(IProjectionBuilderFor<DepotShiftBoard> builder) => builder
        .From<ShiftLogged>(b => b
            .Set(m => m.Depot).To(e => e.Depot)
            .AddChild(m => m.Shifts, child => child
                .IdentifiedBy(_ => _.Worker)
                .UsingKey(e => e.Worker)));
}
