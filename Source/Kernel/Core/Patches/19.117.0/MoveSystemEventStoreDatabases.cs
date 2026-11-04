// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Choreography.Patching;
using Cratis.Chronicle.Storage;
using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle.Patches;

/// <summary>
/// Patch that moves the System event store's data from where it was kept when it was named after the event store to
/// where the configured system database name says it is.
/// </summary>
/// <remarks>
/// Some hosted MongoDB offerings, such as Atlas Flex, reserve a database named <c language="csharp">System</c>, so the System event
/// store's MongoDB databases are now named by <see cref="Configuration.Storage.SystemDatabaseName"/>. Storage that does
/// not name its System event store after configuration has nothing to move.
/// </remarks>
/// <param name="storage"><see cref="IStorage"/> for accessing storage.</param>
/// <param name="logger"><see cref="ILogger"/> for logging.</param>
public class MoveSystemEventStoreDatabases(IStorage storage, ILogger<MoveSystemEventStoreDatabases> logger) : ICanApplyPatch
{
    /// <inheritdoc/>
    public SemanticVersion Version => new(19, 117, 0);

    /// <inheritdoc/>
    public async Task Up()
    {
        logger.MovingToConfiguredLocation();
        await storage.MoveSystemEventStoreTo(SystemEventStoreLocation.Configured);
    }
}
