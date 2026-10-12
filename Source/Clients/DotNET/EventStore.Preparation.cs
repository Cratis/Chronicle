// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Discovery;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.Registrations;

namespace Cratis.Chronicle;

public partial class EventStore
{
    /// <summary>
    /// Gets the current preparation verdict for direct event registration readiness checks.
    /// </summary>
    internal Task LatestPreparationVerdict => _preparation?.LatestVerdict ?? Task.CompletedTask;

    StorePreparationGate Preparation => LazyInitializer.EnsureInitialized(ref _preparation);

    /// <summary>
    /// Rejects registration callback reentry before joining an unfinished task.
    /// </summary>
    internal void ThrowIfArtifactPreparationReentered() => _preparation?.ThrowIfReentered();

    /// <summary>
    /// Detaches a failed store's constructor-installed callback before cache eviction.
    /// </summary>
    internal void DetachAutomaticRegistration() => Connection.Lifecycle.OnConnected -= RegisterAll;

    Task ValidateManualPreparation() => EventTypes is EventTypes native ? native.ValidatePreparation() : Task.CompletedTask;

    async Task DiscoverAllLocal()
    {
        _logger.DiscoverAllArtifacts();
        _storeArtifacts?.Prepare();

        // Finalize identities before schema, migration, source, constraint or observer construction.
        await EventTypes.Discover();
        await EventSources.Discover();
        await Task.WhenAll(
            Constraints.Discover(),
            Reactors.Discover(),
            Reducers.Discover(),
            Projections.Discover(),
            Seeding.Discover());
        var report = BuildDiscoveryReport();
        if (report is not null)
        {
            Discovery = report;
            _logger.LogDiscovery(report, Name, Namespace);
        }
    }
}
