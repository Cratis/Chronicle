// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Integration.Projections.Scenarios.when_projecting_children_with_nested_identities;

/// <summary>
/// Projects briefs into children with nested identities.
/// </summary>
public class NestedIdentityProjection : IProjectionFor<NestedIdentityReadModel>
{
    /// <inheritdoc/>
    public void Define(IProjectionBuilderFor<NestedIdentityReadModel> builder) => builder
        .NoAutoMap()
        .Children(model => model.Children, children => children
            .IdentifiedBy(child => child.Identity.Id)
            .From<NestedIdentityBriefRecorded>(from => from
                .UsingKey(e => e.Brief.Id)
                .UsingParentKeyFromContext(context => context.EventSourceId)
                .Set(child => child.Identity.Id).To(e => e.Brief.Id)
                .Set(child => child.Identity.Topic).To(e => e.Brief.Topic)));
}
