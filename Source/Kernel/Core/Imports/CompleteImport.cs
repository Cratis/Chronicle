// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Commands.ModelBound;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Imports;
using Cratis.Chronicle.Grpc;
using Cratis.Chronicle.Security;

namespace Cratis.Chronicle.Imports;

/// <summary>
/// Represents the command for closing an import's staging and committing it.
/// </summary>
/// <param name="EventStore">The event store the import belongs to.</param>
/// <param name="Namespace">The namespace within the event store.</param>
/// <param name="ImportId">The identifier of the import.</param>
/// <param name="ExpectedChunks">How many chunks the client added; the import fails when staging holds a different number.</param>
/// <param name="ExpectedEvents">How many events the client added; the import fails when staging holds a different number.</param>
/// <remarks>
/// When the counts hold, the import is validated against the constraints as a whole and committed by a job; this
/// returns as soon as that job starts. Observe the import to follow it to completion.
/// </remarks>
[StoreAccess(StoreAccess.Append)]
[Command]
[BelongsTo(WellKnownServices.Imports)]
public record CompleteImport(
    EventStoreName EventStore,
    EventStoreNamespaceName Namespace,
    ImportId ImportId,
    int ExpectedChunks,
    long ExpectedEvents)
{
    /// <summary>
    /// Handles the command.
    /// </summary>
    /// <param name="grainFactory">The <see cref="IGrainFactory"/>.</param>
    /// <returns>The <see cref="ImportStatus"/>.</returns>
    public async Task<ImportStatus> Handle(IGrainFactory grainFactory)
    {
        var key = new ImportKey(EventStore, Namespace, ImportId);
        var result = await grainFactory.GetGrain<IImport>(key).Complete(ExpectedChunks, ExpectedEvents);
        return result.ToStatus(key.ImportId);
    }
}
