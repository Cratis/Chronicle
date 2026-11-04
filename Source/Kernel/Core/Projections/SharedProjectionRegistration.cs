// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Projections;
using Cratis.Chronicle.Concepts.Projections.Definitions;
using Cratis.Chronicle.Setup;

namespace Cratis.Chronicle.Projections;

/// <summary>
/// Shares projection registrations while retaining compatibility with older silos.
/// </summary>
internal static class SharedProjectionRegistration
{
    /// <summary>
    /// Registers through the shared path, falling back when the receiving silo predates it.
    /// </summary>
    /// <param name="manager">The receiving manager.</param>
    /// <param name="signature">The canonical registration signature.</param>
    /// <param name="definitions">The submitted definitions.</param>
    /// <param name="fullSetOwner">The owner of the full set, if any.</param>
    /// <param name="registrant">The registering application, if any.</param>
    /// <param name="version">The application version, if any.</param>
    /// <returns>The registration task.</returns>
    internal static async Task Register(IProjectionsManager manager, string signature, IEnumerable<ProjectionDefinition> definitions, ProjectionOwner? fullSetOwner = null, string? registrant = null, string? version = null)
    {
        try
        {
            await manager.RegisterShared(signature, definitions, fullSetOwner, registrant, version);
        }
        catch (Exception exception) when (IncompatibleSiloRelease.IsLikely(exception))
        {
            // A silo running the previous interface can still accept the ordinary registration.
            await manager.Register(definitions, fullSetOwner, registrant, version);
        }
    }
}
