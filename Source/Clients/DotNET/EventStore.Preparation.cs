// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Discovery;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.Registrations;
using Cratis.Chronicle.Seeding;

namespace Cratis.Chronicle;

public partial class EventStore
{
    /// <summary>
    /// Gets the current preparation verdict for direct event registration readiness checks.
    /// </summary>
    internal Task LatestPreparationVerdict => _preparation?.LatestVerdict ?? Task.CompletedTask;

    StorePreparationGate Preparation => LazyInitializer.EnsureInitialized(ref _preparation, () => new(PublishLocalPreparationFailure));

    /// <summary>
    /// Shares discovered global seed registration with the client's other namespace stores.
    /// </summary>
    /// <param name="registration">The per-client connection registry.</param>
    internal void UseGlobalSeedRegistration(GlobalSeedRegistration registration) =>
        ((EventSeeding)Seeding).UseGlobalSeedRegistration(registration);

    /// <summary>
    /// Rejects registration callback reentry before joining an unfinished task.
    /// </summary>
    internal void ThrowIfArtifactPreparationReentered() => _preparation?.ThrowIfReentered();

    /// <summary>
    /// Detaches a failed store's constructor-installed callback before cache eviction, and stops its background
    /// registration retry so the abandoned store is not kept reachable by that loop.
    /// </summary>
    internal void DetachAutomaticRegistration()
    {
        StopBackgroundRegistrationRetry();
        Connection.Lifecycle.OnConnected -= RegisterAfterConnect;
        Connection.Lifecycle.OnDisconnected -= MarkRegistrationAfterDisconnect;
        if (Occurrences is IDisposable disposable) disposable.Dispose();
    }

    /// <summary>
    /// Stops the background registration retry, now and for any loop started later. Used when the store is detached
    /// and when the owning client is disposed, which does not raise a disconnect that would end the loop.
    /// </summary>
    internal void StopBackgroundRegistrationRetry()
    {
        // Under the same lock as the loop's start, so one of the two always observes the other.
        lock (BackgroundRegistrationRetrySync)
        {
            _isRegistrationDetached = true;
            _backgroundRegistrationRetryCancellation?.Cancel();
        }
    }

    void PublishLocalPreparationFailure(Exception failure)
    {
        lock (RegistrationSync)
        {
            _publishedDisconnectEpoch = _disconnectEpoch;
            Registration = new RegistrationOutcome(true, _projections.ArtifactRegistrations, failure)
            {
                IdentityDrift = Registration.IdentityDrift,
                IsRegistering = _registrationTask is not null
            };
        }
    }

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
            Seeding.Discover(),
            Occurrences is Occurrences.Occurrences occurrences ? occurrences.Discover() : Task.CompletedTask);
        var report = BuildDiscoveryReport();
        if (report is not null)
        {
            Discovery = report;
            _logger.LogDiscovery(report, Name, Namespace);
        }
    }
}
