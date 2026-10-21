// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using System.Runtime.ExceptionServices;
using Cratis.Chronicle.Contracts.Namespaces;
using Cratis.Chronicle.Contracts.Queries;
using Cratis.Chronicle.Discovery;
using Cratis.Chronicle.Registrations;

namespace Cratis.Chronicle;

public partial class EventStore
{
    object? _registrationSync;
    Task? _registrationTask;
    long _disconnectEpoch;
    long _publishedDisconnectEpoch;

    /// <summary>
    /// Gets whether this event store was retired because its namespace was removed while the client was disconnected.
    /// A retired event store does not register again, and the client creates a new one if the namespace is used again.
    /// </summary>
    internal bool IsRetired { get; private set; }

    object BackgroundRegistrationRetrySync => LazyInitializer.EnsureInitialized(ref _backgroundRegistrationRetrySyncObject);

    object RegistrationSync => LazyInitializer.EnsureInitialized(ref _registrationSync);

    long DisconnectEpoch
    {
        get
        {
            lock (RegistrationSync)
            {
                return _disconnectEpoch;
            }
        }
    }

    /// <summary>
    /// Register after the connection was made, unless this is a reconnect and the namespace was removed meanwhile.
    /// </summary>
    /// <returns>Awaitable task.</returns>
    internal async Task RegisterAfterConnect()
    {
        // Registering in a namespace creates it. After a reconnect that would bring back a namespace that was removed
        // while this client was away - with its observers and, on first use, its data - so a namespace the kernel no
        // longer lists is let go instead. Failing to ask is not a reason to let go: register as usual.
        if (Interlocked.Read(ref _disconnectEpoch) > 0 && Namespace != EventStoreNamespaceName.Default && await WasNamespaceRemoved())
        {
            IsRetired = true;
            DetachAutomaticRegistration();
            return;
        }

        await RegisterAll();
    }

    async Task<bool> WasNamespaceRemoved()
    {
        try
        {
            var namespaces = await _servicesAccessor.Services.Namespaces.AllNamespaces(new AllNamespacesRequest { EventStore = _eventStoreName }).EnsureSuccess();
            return !namespaces.Any(_ => string.Equals(_.Name, Namespace.Value, StringComparison.OrdinalIgnoreCase));
        }
        catch (Exception)
        {
            return false;
        }
    }

    Task MarkRegistrationAfterDisconnect()
    {
        lock (RegistrationSync)
        {
            _disconnectEpoch++;
            Registration = Registration with { IsRegistering = true };
        }

        return Task.CompletedTask;
    }

    async Task CompleteRegistration()
    {
        // Publish the shared task before any work can complete, including constructor-free specification stores.
        await Task.Yield();
        var flight = LazyInitializer.EnsureInitialized(ref _registerAllFlight, () => new SingleFlightRegistration(RegisterAllCore));
        while (true)
        {
            Exception? failure = null;
            try
            {
                await flight.Run();
            }
            catch (Exception exception)
            {
                failure = exception;
            }

            lock (RegistrationSync)
            {
                // Also cover a reconnect arriving after publication but before SingleFlightRegistration releases
                // its task. Otherwise that caller joins a finished registration and leaves the flag stuck true.
                if (_publishedDisconnectEpoch != _disconnectEpoch && Connection.Lifecycle.IsConnected)
                {
                    continue;
                }

                Registration = Registration with { IsRegistering = _publishedDisconnectEpoch != _disconnectEpoch };
                _registrationTask = null;
            }

            if (failure is not null)
            {
                ExceptionDispatchInfo.Capture(failure).Throw();
            }

            return;
        }
    }

    bool PublishRegistration(long epoch, IImmutableList<ArtifactIdentityDrift> drift, Exception? runFailure)
    {
        var published = false;

        // Preparation owns the outer lock: its local-failure callback also takes RegistrationSync.
        Preparation.PublishRegistrationOutcome(
            failure =>
            {
                var localFailure = Preparation.LatestVerdict.IsFaulted;
                lock (RegistrationSync)
                {
                    if (epoch != _disconnectEpoch && Connection.Lifecycle.IsConnected && !localFailure &&
                        failure is not (ArtifactIdentityDriftDetected or ArtifactPreparationReentered))
                    {
                        return;
                    }

                    _publishedDisconnectEpoch = epoch;
                    Registration = new RegistrationOutcome(true, _projections.ArtifactRegistrations, failure)
                    {
                        IdentityDrift = drift,
                        IsRegistering = epoch != _disconnectEpoch
                    };
                    published = true;
                }
            },
            runFailure);

        return published;
    }
}
