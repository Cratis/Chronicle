// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Integration.for_EventSequence.given;

/// <summary>
/// Accumulates contributions using an event-property key rather than the event source.
/// </summary>
public class CoupledContentMutationProjection : IProjectionFor<CoupledContentMutationDocument>
{
    /// <inheritdoc/>
    public void Define(IProjectionBuilderFor<CoupledContentMutationDocument> builder) => builder
        .From<ContentContributionRecorded>(from => from
            .UsingKey(@event => @event.DocumentId)
            .Add(model => model.Total).With(@event => @event.Amount));
}
