// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.Occurrences;

/// <summary>
/// Defines event-store-level storage for trigger payload contracts.
/// </summary>
public interface ITriggerDefinitionsStorage
{
    /// <summary>
    /// Reads a trigger's current payload contract.
    /// </summary>
    /// <param name="id">The stable trigger identity.</param>
    /// <returns>The definition, or null if it is not registered.</returns>
    Task<TriggerDefinition?> Get(TriggerId id);

    /// <summary>
    /// Enumerates registered triggers for rehydration.
    /// </summary>
    /// <returns>The registered definitions.</returns>
    Task<IEnumerable<TriggerDefinition>> GetAll();

    /// <summary>
    /// Conditionally saves a revision. A null expected revision means insert-if-absent.
    /// </summary>
    /// <param name="definition">The next definition.</param>
    /// <param name="expectedRevision">The revision observed by the registrar, or null for a new trigger.</param>
    /// <returns>Whether the definition was saved.</returns>
    Task<bool> TrySave(TriggerDefinition definition, long? expectedRevision);
}
