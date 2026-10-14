// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using System.Runtime.ExceptionServices;
using Cratis.Chronicle.Discovery;
using Cratis.Chronicle.Registrations;

namespace Cratis.Chronicle;

public partial class EventStore
{
    object? _registrationSync;
    Task? _registrationTask;
    long _disconnectEpoch;
    long _publishedDisconnectEpoch;

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
