// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Projections;

namespace Cratis.Chronicle.Testing.ReadModels.for_ReadModelScenario.when_projecting_children_with_a_constant_parent_key.given;

/// <summary>
/// Counts at the root and keeps a membership set of children, both keyed on the same constant.
/// </summary>
public class CountsProjection : IProjectionFor<CountsReadModel>
{
    /// <summary>
    /// The constant both the root and its children are keyed on.
    /// </summary>
    public const string ConstantKeyValue = "counts";

    /// <inheritdoc/>
    public void Define(IProjectionBuilderFor<CountsReadModel> builder) => builder
        .From<CountedThingRegistered>(_ => _
            .UsingConstantKey(ConstantKeyValue)
            .Count(m => m.Registrations))
        .Children(m => m.Things, children => children
            .IdentifiedBy(child => child.Id)
            .From<CountedThingRegistered>(_ => _
                .UsingConstantParentKey(ConstantKeyValue)
                .Set(child => child.Label).To(e => e.Label)
                .Set(child => child.IsOpen).To(e => e.IsOpen))
            .From<CountedThingClosed>(_ => _
                .UsingConstantParentKey(ConstantKeyValue)
                .Set(child => child.IsOpen).ToValue(false))
            .RemovedWith<CountedThingRemoved>(_ => _
                .UsingConstantParentKey(ConstantKeyValue)));
}
