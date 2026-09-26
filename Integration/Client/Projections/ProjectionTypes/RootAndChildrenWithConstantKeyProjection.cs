// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Integration.Projections.Events;
using Cratis.Chronicle.Integration.Projections.ReadModels;

namespace Cratis.Chronicle.Integration.Projections.ProjectionTypes;

/// <summary>
/// The same fixed-key document, but the root counts on its own account as well as holding children.
/// </summary>
/// <remarks>
/// A counting document usually wants both: a monotonic total that nothing ever leaves, and a membership
/// set for the states things enter and leave. Splitting those across two read models is the workaround
/// this projection exists to make unnecessary.
/// </remarks>
public class RootAndChildrenWithConstantKeyProjection : IProjectionFor<CountsReadModel>
{
    /// <summary>
    /// The constant both the root and its children are keyed on.
    /// </summary>
    public const string ConstantKeyValue = "root-and-children-constant-key";

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
