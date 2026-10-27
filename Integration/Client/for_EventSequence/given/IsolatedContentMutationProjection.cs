// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Integration.for_EventSequence.given;

/// <summary>
/// Accumulates contributions independently for each event source.
/// </summary>
public class IsolatedContentMutationProjection : IProjectionFor<IsolatedContentMutationDocument>
{
    /// <inheritdoc/>
    public void Define(IProjectionBuilderFor<IsolatedContentMutationDocument> builder) => builder
        .From<ContentContributionRecorded>(from => from.Add(model => model.Total).With(@event => @event.Amount));
}
