// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.System;
using Cratis.Chronicle.Patching;
using Cratis.Chronicle.Storage;
using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle.Patches;

/// <summary>
/// Patch that moves the System event store's data to its configured location once more.
/// </summary>
/// <remarks>
/// <see cref="MoveSystemEventStoreDatabases"/> ran as a patch before the storage the kernel registers could do the
/// move, so it logged that it was moving the data, did nothing, and was recorded as applied. The kernel then kept the
/// System event store in the configured location, empty, while the data stayed where it always was. The move leaves
/// whatever the kernel wrote to the configured location in place, copies what is missing and drops each legacy database
/// once its copy is confirmed, so doing it again is safe where it already happened, and has nothing to do where the
/// legacy names are still configured.
/// </remarks>
/// <param name="storage"><see cref="IStorage"/> for accessing storage.</param>
/// <param name="logger"><see cref="ILogger"/> for logging.</param>
public class RepeatMoveSystemEventStoreDatabases(IStorage storage, ILogger<RepeatMoveSystemEventStoreDatabases> logger) : ICanApplyPatch
{
    /// <inheritdoc/>
    public SemanticVersion Version => new(19, 124, 0);

    /// <inheritdoc/>
    public async Task Up()
    {
        logger.MovingToConfiguredLocation();
        await storage.MoveSystemEventStoreTo(SystemEventStoreLocation.Configured);
    }

    /// <inheritdoc/>
    public Task Down() => Task.CompletedTask;
}
