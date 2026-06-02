// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Api.SequenceQueries.Events;
using Cratis.Chronicle.Projections;

namespace Cratis.Chronicle.Api.SequenceQueries.Listing;

/// <summary>
/// Projection that builds <see cref="EventSequenceQueryFolder"/> instances from the folder and
/// query events. The shared folder variant sets <see cref="EventSequenceQueryFolder.Owner"/> to the
/// system literal; the per-user variant sets it from the event context's caused-by subject. Queries
/// added to a folder land as nested children in <see cref="EventSequenceQueryFolder.Queries"/>.
/// </summary>
/// <remarks>
/// Equivalent to the following PDL:
/// <code>
/// projection EventSequenceQueryFolderProjection => EventSequenceQueryFolder
///   from EventSequenceQueryFolderAdded
///     key $eventSourceId
///     name = name
///     owner = "System"
///   from EventSequenceQueryFolderForUserAdded
///     key $eventSourceId
///     name = name
///     owner = $causedBy.subject
///   children queries identified by queryId
///     from EventSequenceQueryAdded
///       key $eventSourceId
///       parent folderId
///       name = name
///       eventSequenceId = eventSequenceId
///       filter = filter
/// </code>
/// </remarks>
public class EventSequenceQueryFolderProjection : IProjectionFor<EventSequenceQueryFolder>
{
    /// <summary>
    /// The literal owner value used for shared folders.
    /// </summary>
    public const string SystemOwner = "System";

    /// <inheritdoc/>
    public void Define(IProjectionBuilderFor<EventSequenceQueryFolder> builder) => builder
        .From<EventSequenceQueryFolderAdded>(_ => _
            .Set(folder => folder.Owner).ToValue(SystemOwner))
        .From<EventSequenceQueryFolderForUserAdded>(_ => _
            .Set(folder => folder.Owner).ToEventContextProperty(context => context.CausedBy.Subject))
        .Children(folder => folder.Queries, children => children
            .IdentifiedBy(query => query.QueryId)
            .From<EventSequenceQueryAdded>(_ => _
                .UsingParentKey(@event => @event.FolderId)));
}
