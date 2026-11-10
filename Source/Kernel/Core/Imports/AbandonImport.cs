// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Commands.ModelBound;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Imports;
using Cratis.Chronicle.Grpc;
using Cratis.Chronicle.Security;

namespace Cratis.Chronicle.Imports;

/// <summary>
/// Represents the command for abandoning an import before its commit starts, discarding its staging.
/// </summary>
/// <param name="EventStore">The event store the import belongs to.</param>
/// <param name="Namespace">The namespace within the event store.</param>
/// <param name="ImportId">The identifier of the import.</param>
[StoreAccess(StoreAccess.Append)]
[Command]
[BelongsTo(WellKnownServices.Imports)]
public record AbandonImport(EventStoreName EventStore, EventStoreNamespaceName Namespace, ImportId ImportId)
{
    /// <summary>
    /// Handles the command.
    /// </summary>
    /// <param name="grainFactory">The <see cref="IGrainFactory"/>.</param>
    /// <returns>The <see cref="ImportStatus"/>.</returns>
    public async Task<ImportStatus> Handle(IGrainFactory grainFactory)
    {
        var key = new ImportKey(EventStore, Namespace, ImportId);
        var result = await grainFactory.GetGrain<IImport>(key).Abandon("The import was abandoned by its client.");
        return result.ToStatus(key.ImportId);
    }
}
