// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Connections;
using Cratis.Chronicle.Contracts.EventStores;
using Cratis.Chronicle.Contracts.Queries;
using Cratis.Chronicle.Registrations;
using Grpc.Core;
using ProtoBuf.Grpc;

namespace Cratis.Chronicle;

public partial class EventStore
{
    /// <summary>
    /// Reads deployment metadata without registering any artifacts.
    /// </summary>
    /// <param name="cancellationToken">Cancellation for the diagnostic.</param>
    /// <returns>The stored deployment metadata.</returns>
    internal async Task<EventStoreDeploymentResponse> GetDeployment(CancellationToken cancellationToken)
    {
        var services = await _servicesAccessor.GetServices(cancellationToken);
        var query = services.EventStores.GetEventStoreDeployment(
            new() { EventStore = Name.Value },
            new CallContext(new CallOptions(cancellationToken: cancellationToken))).EnsureSuccess();

        // A custom transport may ignore cancellation. Observe failures even after this await is cancelled.
        _ = query.ContinueWith(task => _ = task.Exception, CancellationToken.None, TaskContinuationOptions.OnlyOnFaulted | TaskContinuationOptions.ExecuteSynchronously, TaskScheduler.Default);
        return await query.WaitAsync(cancellationToken);
    }

    /// <summary>
    /// Computes the current prepared definitions' fingerprint without registration effects or live event schema mutation.
    /// </summary>
    /// <returns>The diagnostic fingerprint.</returns>
    internal async Task<ArtifactFingerprint> ComputeDefinitionsFingerprint()
    {
        var fingerprint = ArtifactFingerprint.NotSet;
        await Preparation.Register(static () => Task.CompletedTask, async () =>
        {
            // Validation must be in the callback: a failed initializer would publish a failed store revision.
            await ValidateManualPreparation();
            var payloads = ArtifactDefinitionPayloads.Empty;
            if (EventTypes is Events.EventTypes eventTypes)
            {
                var request = await eventTypes.BuildDeploymentPayloadSnapshot();
                payloads = payloads with { EventTypes = request.Types };
            }
            if (EventSources is EventSources.EventSources eventSources)
            {
                payloads = payloads with { EventSources = eventSources.PrepareRegistration()?.Sources ?? [] };
            }
            if (ReadModels is ReadModels.ReadModels readModels)
            {
                payloads = payloads with { ReadModels = readModels.PrepareRegistration().ReadModels };
            }
            if (Constraints is Events.Constraints.Constraints constraints)
            {
                payloads = payloads with { Constraints = constraints.PrepareRegistration().Constraints };
            }
            if (Projections is Projections.Projections projections)
            {
                payloads = payloads with { Projections = projections.PrepareRegistration().Projections };
            }
            var unregistered = _projections.ArtifactRegistrations.Where(_ => !_.IsRegistered).ToArray();
            if (unregistered.Length > 0)
            {
                throw new ArtifactDefinitionsIncomplete(unregistered);
            }
            fingerprint = ArtifactFingerprintCalculator.Calculate(payloads);
        });
        return fingerprint;
    }
}
