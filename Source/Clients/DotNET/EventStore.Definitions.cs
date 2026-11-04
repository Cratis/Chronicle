// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using System.Runtime.ExceptionServices;
using Cratis.Chronicle.Connections;
using Cratis.Chronicle.Contracts.Commands;
using Cratis.Chronicle.Discovery;
using Cratis.Chronicle.Registrations;

namespace Cratis.Chronicle;

public partial class EventStore
{
    /// <inheritdoc/>
    public async Task<DefinitionsRegistration> RegisterDefinitions(DeploymentGeneration? generation = default, CancellationToken cancellationToken = default)
    {
        generation ??= DeploymentGeneration.NotSet;
        if (generation.Value > long.MaxValue)
        {
            throw new DeploymentGenerationOutOfRange(generation);
        }

        Preparation.ThrowIfReentered();
        var services = await _servicesAccessor.GetServices(cancellationToken);
        var fingerprint = ArtifactFingerprint.NotSet;
        var drift = ImmutableList<ArtifactIdentityDrift>.Empty;
        Exception? failure = null;
        try
        {
            await Preparation.Register(DiscoverAllLocal, async () =>
            {
                _logger.RegisteringDefinitions(_eventStoreName, generation);
                var prepared = await PrepareDefinitionPayloads();
                var unregistered = _projections.ArtifactRegistrations.Where(_ => !_.IsRegistered).ToArray();
                if (unregistered.Length > 0)
                {
                    throw new ArtifactDefinitionsIncomplete(unregistered);
                }

                drift = (await DetectIdentityDrift()).ToImmutableList();
                ThrowIfIdentityDriftMustFail(drift);
                fingerprint = ArtifactFingerprintCalculator.Calculate(prepared.Payloads);
                Preparation.VerifyRegistrationLease();
                cancellationToken.ThrowIfCancellationRequested();
                (await services.EventStores.EnsureEventStore(new() { Name = _eventStoreName })).EnsureSuccess();
                var result = await services.EventStores.RegisterDeployment(new()
                {
                    EventStore = _eventStoreName,
                    Generation = generation.Value,
                    Fingerprint = fingerprint.Value
                });
                if (!result.IsSuccess && result.ValidationResults.Any(_ => _.Members.Any(member => string.Equals(member, "generation", StringComparison.OrdinalIgnoreCase))))
                {
                    throw new DeploymentGenerationIsStale(_eventStoreName, generation, string.Join("; ", result.ValidationResults.Select(_ => _.Message)));
                }

                result.EnsureSuccess();
                cancellationToken.ThrowIfCancellationRequested();
                await prepared.SendFirstWave();
                cancellationToken.ThrowIfCancellationRequested();
                await prepared.SendSecondWave();
                _logger.DefinitionsRegistered(_eventStoreName, generation, fingerprint);
            });
        }
        catch (Exception exception)
        {
            failure = exception;
        }

        Preparation.PublishRegistrationOutcome(
            publishedFailure =>
            {
                lock (RegistrationSync)
                {
                    Registration = new RegistrationOutcome(true, _projections.ArtifactRegistrations, publishedFailure)
                    {
                        IdentityDrift = drift,
                        IsRegistering = _registrationTask is not null,
                        Fingerprint = fingerprint
                    };
                }
            },
            failure);
        if (failure is not null)
        {
            ExceptionDispatchInfo.Capture(failure).Throw();
        }

        return new(fingerprint, generation);
    }

    async Task<(ArtifactDefinitionPayloads Payloads, Func<Task> SendFirstWave, Func<Task> SendSecondWave)> PrepareDefinitionPayloads()
    {
        var payloads = ArtifactDefinitionPayloads.Empty;
        Func<Task> sendEventTypes = EventTypes.Register;
        Func<Task> sendEventSources = EventSources.Register;
        Func<Task> sendReadModels = ReadModels.Register;
        Func<Task> sendConstraints = Constraints.Register;
        Func<Task> sendProjections = Projections.Register;
        try
        {
            if (EventTypes is Events.EventTypes eventTypes)
            {
                var request = await eventTypes.PrepareRegistration();
                payloads = payloads with { EventTypes = request.Types };
                sendEventTypes = () => eventTypes.RegisterPrepared(request);
            }

            if (EventSources is EventSources.EventSources eventSources)
            {
                var request = eventSources.PrepareRegistration();
                payloads = payloads with { EventSources = request?.Sources ?? [] };
                sendEventSources = () => request is null ? Task.CompletedTask : eventSources.RegisterPrepared(request);
            }

            if (ReadModels is ReadModels.ReadModels readModels)
            {
                var request = readModels.PrepareRegistration();
                payloads = payloads with { ReadModels = request.ReadModels };
                sendReadModels = () => readModels.RegisterPrepared(request);
            }

            if (Constraints is Events.Constraints.Constraints constraints)
            {
                var request = constraints.PrepareRegistration();
                payloads = payloads with { Constraints = request.Constraints };
                sendConstraints = () => constraints.RegisterPrepared(request);
            }

            if (Projections is Projections.Projections projections)
            {
                var request = projections.PrepareRegistration();
                payloads = payloads with { Projections = request.Projections };
                sendProjections = () => projections.RegisterPrepared(request);
            }
        }
        catch (Exception exception)
        {
            Preparation.RecordPayloadFailure(exception);
            throw;
        }

        return (payloads,
            () => Task.WhenAll(sendEventTypes(), sendEventSources(), sendReadModels()),
            () => Task.WhenAll(sendConstraints(), sendProjections()));
    }
}
