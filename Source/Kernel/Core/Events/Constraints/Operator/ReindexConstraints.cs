// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Commands.ModelBound;
using Cratis.Choreography.Jobs;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Events.Constraints;
using Cratis.Chronicle.Concepts.EventSequences;
using Cratis.Chronicle.Grpc;

namespace Cratis.Chronicle.Events.Constraints.Operator;

/// <summary>
/// Requests an explicit rebuild of registered constraint indexes from stored event history.
/// </summary>
/// <param name="EventStore">The event store holding the definitions.</param>
/// <param name="Namespace">The namespace whose indexes are rebuilt.</param>
/// <param name="EventSequenceId">The event sequence to replay.</param>
/// <param name="ConstraintNames">The names to rebuild; empty selects every applicable unique and closing constraint.</param>
[Command]
[Cratis.Arc.Authorization.Authorize(Policy = Security.WellKnownAuthorizationPolicies.AdministratorOperations)]
[Security.DestructiveOperation]
[BelongsTo(WellKnownServices.ConstraintIndexes)]
public record ReindexConstraints(EventStoreName EventStore, EventStoreNamespaceName Namespace, EventSequenceId EventSequenceId, IEnumerable<string> ConstraintNames)
{
    /// <summary>
    /// Refreshes live constraint validation and starts the existing durable reindex job.
    /// </summary>
    /// <param name="grainFactory">The factory for registered definitions.</param>
    /// <param name="indexes">The shared coordinator for refreshing and rebuilding constraint indexes.</param>
    /// <returns>The identifier of the started job.</returns>
    /// <exception cref="ConstraintReindexCouldNotBeStarted">The selection is invalid or the durable job could not be started.</exception>
    public async Task<JobId> Handle(IGrainFactory grainFactory, IConstraintIndexes indexes)
    {
        var definitions = await grainFactory.GetGrain<IConstraints>(EventStore).GetDefinitions();
        var selection = ConstraintReindexSelection.For(this, definitions);
        if (selection.Error is not null) throw new ConstraintReindexCouldNotBeStarted(selection.Error);

        var changes = selection.Definitions.Select(definition => new ConstraintDefinitionChange(definition.Name, true, [ConstraintChangeType.IndexedPropertiesChanged])).ToArray();
        return await indexes.Reindex(EventStore, Namespace, EventSequenceId, changes);
    }
}
