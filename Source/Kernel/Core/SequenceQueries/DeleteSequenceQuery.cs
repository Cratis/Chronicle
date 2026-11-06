// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Authorization;
using Cratis.Arc.Commands.ModelBound;
using Cratis.Arc.Validation;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.SequenceQueries;
using Cratis.Chronicle.Security;
using Cratis.Chronicle.Storage;
using Cratis.Chronicle.Storage.SequenceQueries;

namespace Cratis.Chronicle.SequenceQueries;

/// <summary>
/// Represents the command for deleting a saved event sequence query.
/// </summary>
/// <param name="EventStore">The event store the query belongs to.</param>
/// <param name="Id">The unique identifier of the query to delete.</param>
[Command]
public record DeleteSequenceQuery(EventStoreName EventStore, SequenceQueryId Id)
{
    /// <summary>
    /// Handles the command.
    /// </summary>
    /// <param name="currentPrincipalAccessor"><see cref="ICurrentPrincipalAccessor"/> for resolving the caller.</param>
    /// <param name="administratorAuthorization">The <see cref="IAdministratorAuthorization"/> for the administrator override.</param>
    /// <param name="storage">The <see cref="IStorage"/> holding the saved queries.</param>
    /// <returns>Null when the change was applied, otherwise the refusal.</returns>
    public async Task<ValidationResult?> Handle(ICurrentPrincipalAccessor currentPrincipalAccessor, IAdministratorAuthorization administratorAuthorization, IStorage storage)
    {
        var queries = storage.GetEventStore(EventStore).SequenceQueries;

        return await SequenceQueryChangeAccess.Change(
            () => queries.Get(Id),
            existing => existing.Owner,
            existing => existing is null
                ? Task.FromResult(SequenceQueryWriteOutcome.Applied)
                : queries.Delete(Id, existing.Owner),
            SequenceQueryChangeAccess.QueryRefusal,
            currentPrincipalAccessor,
            administratorAuthorization);
    }
}
