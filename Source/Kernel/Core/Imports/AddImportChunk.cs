// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Authorization;
using Cratis.Arc.Commands.ModelBound;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Imports;
using Cratis.Chronicle.Grpc;
using Cratis.Chronicle.Security;
using Cratis.Chronicle.Sequences;

namespace Cratis.Chronicle.Imports;

/// <summary>
/// Represents the command for adding one chunk of events to an import session.
/// </summary>
/// <param name="EventStore">The event store the import belongs to.</param>
/// <param name="Namespace">The namespace within the event store.</param>
/// <param name="ImportId">The identifier of the import.</param>
/// <param name="ChunkIndex">The index of the chunk, from zero. Adding the same index again returns the first result.</param>
/// <param name="FirstEventIndex">The index in the import of the first event in the chunk, so rejections name the event.</param>
/// <param name="Events">The events of the chunk.</param>
/// <remarks>
/// Staging happens on a stateless worker, outside the event sequence, so chunks may be added in parallel. An event
/// whose caused-by identity is not given is recorded as caused by the caller; a Chronicle user is always recorded as
/// the caller, as for an ordinary append.
/// </remarks>
[StoreAccess(StoreAccess.Append)]
[Command]
[BelongsTo(WellKnownServices.Imports)]
public record AddImportChunk(
    EventStoreName EventStore,
    EventStoreNamespaceName Namespace,
    ImportId ImportId,
    int ChunkIndex,
    long FirstEventIndex,
    IEnumerable<ImportEvent> Events)
{
    /// <summary>
    /// Handles the command.
    /// </summary>
    /// <param name="grainFactory">The <see cref="IGrainFactory"/>.</param>
    /// <param name="principalAccessor">The <see cref="ICurrentPrincipalAccessor"/> resolving who is importing.</param>
    /// <returns>The <see cref="ImportChunkResult"/>.</returns>
    public async Task<ImportChunkResult> Handle(IGrainFactory grainFactory, ICurrentPrincipalAccessor principalAccessor)
    {
        var events = Events.ToArray();
        var caller = principalAccessor.Current;
        var causedBy = events.Select(_ => caller.ResolveCausedBy(_.CausedBy)).ToArray();
        var result = await grainFactory.GetGrain<IImportStager>(0).Stage(new ImportKey(EventStore, Namespace, ImportId), ChunkIndex, FirstEventIndex, events, causedBy);
        return result.ToWire();
    }
}
