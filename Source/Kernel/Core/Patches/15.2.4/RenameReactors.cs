// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Observation.Reactors;
using Cratis.Chronicle.Concepts.System;
using Cratis.Chronicle.Patching;
using Cratis.Chronicle.Storage;
using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle.Patches;

/// <summary>
/// Patch to rename reactors by removing 'Grains' from their names.
/// </summary>
/// <param name="storage"><see cref="IStorage"/> for accessing storage.</param>
/// <param name="logger"><see cref="ILogger"/> for logging.</param>
public class RenameReactors(IStorage storage, ILogger<RenameReactors> logger) : ICanApplyPatch
{
    /// <inheritdoc/>
    public SemanticVersion Version => new(15, 2, 4);

    /// <inheritdoc/>
    public async Task Up()
    {
        logger.StartingPatch();

        var systemEventStore = storage.GetEventStore(EventStoreName.System);
        var reactors = await systemEventStore.Reactors.GetAll();

        var reactorsToRename = reactors.Where(r => r.Identifier.Value.Contains("Grains", StringComparison.OrdinalIgnoreCase)).ToList();

        logger.FoundReactorsToRename(reactorsToRename.Count);

        foreach (var reactor in reactorsToRename)
        {
            var currentId = reactor.Identifier;
            var newIdValue = currentId.Value.Replace("Grains.", string.Empty, StringComparison.OrdinalIgnoreCase);
            var newId = new ReactorId(newIdValue);

            logger.RenamingReactor(currentId, newId);
            await systemEventStore.Reactors.Rename(currentId, newId);
        }

        logger.PatchCompleted();
    }

    /// <inheritdoc/>
    public async Task Down()
    {
        logger.StartingRollback();

        var systemEventStore = storage.GetEventStore(EventStoreName.System);
        var reactors = await systemEventStore.Reactors.GetAll();

        foreach (var reactor in reactors)
        {
            var currentId = reactor.Identifier;
            var parts = currentId.Value.Split('.');
            if (parts.Length > 0)
            {
                var lastPart = parts[^1];
                if (!lastPart.Contains("Grains", StringComparison.OrdinalIgnoreCase))
                {
                    parts[^1] = $"Grains.{lastPart}";
                    var newIdValue = string.Join('.', parts);
                    var newId = new ReactorId(newIdValue);

                    logger.RestoringReactorName(currentId, newId);
                    await systemEventStore.Reactors.Rename(currentId, newId);
                }
            }
        }

        logger.RollbackCompleted();
    }
}
