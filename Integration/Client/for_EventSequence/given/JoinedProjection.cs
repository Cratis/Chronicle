// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Integration.for_EventSequence.given;

/// <summary>
/// Materializes documents and joins their reference values across event sources.
/// </summary>
public class JoinedProjection : IProjectionFor<JoinedReadModel>
{
    /// <inheritdoc/>
    public void Define(IProjectionBuilderFor<JoinedReadModel> builder) => builder
        .From<CrossSourceDocumentLinked>(from => from
            .UsingKey(@event => @event.DocumentId)
            .Set(model => model.ReferenceId).To(@event => @event.ReferenceId))
        .Join<CrossSourceReferenceChanged>(join => join
            .On(model => model.ReferenceId)
            .Set(model => model.Amount).To(@event => @event.Amount));
}
