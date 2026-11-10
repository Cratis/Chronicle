// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Commands.ModelBound;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Imports;
using Cratis.Chronicle.Configuration;
using Cratis.Chronicle.Grpc;
using Cratis.Chronicle.Security;
using Microsoft.Extensions.Options;

namespace Cratis.Chronicle.Imports;

/// <summary>
/// Represents the command for beginning an import session into an event sequence (decision 0023).
/// </summary>
/// <param name="EventStore">The event store the sequence belongs to.</param>
/// <param name="Namespace">The namespace within the event store.</param>
/// <param name="EventSequenceId">The event sequence the import targets.</param>
/// <param name="OnInvalid">What the import does with invalid events; by default it rejects the whole import.</param>
/// <param name="ImportId">Optional identifier of the import, so that beginning it again returns the same session. A new one is created when not given.</param>
[StoreAccess(StoreAccess.Append)]
[Command]
[BelongsTo(WellKnownServices.Imports)]
public record BeginImport(
    EventStoreName EventStore,
    EventStoreNamespaceName Namespace,
    Concepts.EventSequences.EventSequenceId EventSequenceId,
    OnInvalidEvent OnInvalid = OnInvalidEvent.Reject,
    Guid? ImportId = default)
{
    /// <summary>
    /// Handles the command.
    /// </summary>
    /// <param name="grainFactory">The <see cref="IGrainFactory"/>.</param>
    /// <param name="options">The <see cref="ChronicleOptions"/>.</param>
    /// <returns>The <see cref="ImportSession"/>.</returns>
    public async Task<ImportSession> Handle(IGrainFactory grainFactory, IOptions<ChronicleOptions> options)
    {
        var importId = ImportId is { } id && id != Guid.Empty ? new ImportId(id) : Concepts.Imports.ImportId.New();
        var limits = options.Value.Imports;
        var result = await grainFactory.GetGrain<IImport>(new ImportKey(EventStore, Namespace, importId)).Begin(EventSequenceId, OnInvalid);
        return new(importId.Value, result.Record?.State ?? ImportState.Failed, limits.MaxChunkEvents, limits.MaxChunkBytes, result.Error);
    }
}
