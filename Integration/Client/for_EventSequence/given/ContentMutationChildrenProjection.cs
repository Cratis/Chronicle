// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Integration.for_EventSequence.given;

/// <summary>
/// Routes child contributions from distinct sources into an event-property-keyed parent.
/// </summary>
public class ContentMutationChildrenProjection : IProjectionFor<ContentMutationChildrenDocument>
{
    /// <inheritdoc/>
    public void Define(IProjectionBuilderFor<ContentMutationChildrenDocument> builder) => builder
        .Children(model => model.Items, children => children
            .IdentifiedBy(child => child.Id)
            .From<ContentContributionRecorded>(from => from
                .UsingParentKey(@event => @event.DocumentId)
                .Set(child => child.Amount).To(@event => @event.Amount)));
}
