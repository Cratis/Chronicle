// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Cryptography;
using System.Text.Json;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Configuration;
using Cratis.Chronicle.Storage;
using Cratis.Chronicle.Storage.Compliance;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Orleans.Configuration;

namespace Cratis.Chronicle.Compliance;

/// <summary>Publishes protocol-N silo records. It does not create or infer a ProtocolFloor attestation.</summary>
/// <param name="storage">The namespace catalog.</param>
/// <param name="keys">The complete key storage composition.</param>
/// <param name="authority">The backend authority.</param>
/// <param name="silo">The current silo incarnation.</param>
/// <param name="cluster">The configured cluster identity.</param>
/// <param name="membership">The membership heartbeat cadence.</param>
/// <param name="options">The configured grace.</param>
/// <param name="logger">Non-sensitive startup diagnostics.</param>
/// <param name="timeProvider">The heartbeat clock.</param>
public sealed class EncryptionKeyFenceProtocol(
    IStorage storage,
    IEncryptionKeyStorage keys,
    IEncryptionKeyFence authority,
    ILocalSiloDetails silo,
    IOptions<ClusterOptions> cluster,
    IOptions<ClusterMembershipOptions> membership,
    IOptions<ChronicleOptions> options,
    ILogger<EncryptionKeyFenceProtocol> logger,
    TimeProvider timeProvider) : ILifecycleParticipant<ISiloLifecycle>, IDisposable
{
    readonly CancellationTokenSource _stop = new();
    Task _heartbeats = Task.CompletedTask;

    /// <inheritdoc/>
    public void Participate(ISiloLifecycle lifecycle) => lifecycle.Subscribe(
        nameof(EncryptionKeyFenceProtocol), ServiceLifecycleStage.Active, Start, Stop);

    /// <inheritdoc/>
    public void Dispose()
    {
        _stop.Cancel();
        _stop.Dispose();
    }

    /// <summary>Publish this incarnation to each known namespace authority.</summary>
    /// <returns>Awaitable publication.</returns>
    internal async Task Publish()
    {
        if (authority is not IEncryptionKeyFenceRecords records)
        {
            logger.MissingProtocolAuthority();
            return;
        }

        var identity = new[] { cluster.Value.ClusterId, silo.SiloAddress.ToParsableString() };
        var id = "protocol/" + Convert.ToHexStringLower(SHA256.HashData(JsonSerializer.SerializeToUtf8Bytes(identity)));
        var value = JsonSerializer.Serialize(new
        {
            ClusterId = identity[0],
            Silo = identity[1],
            BuildVersion = typeof(EncryptionKeyFenceProtocol).Assembly.GetName().Version?.ToString() ?? "unknown",
            Protocol = 1,
            Timestamp = timeProvider.GetUtcNow()
        });

        // Silo publication must not depend on there being any user event stores or namespaces yet.
        await records.WriteRecord(EventStoreName.System, EventStoreNamespaceName.Default, id, value, replace: true);
        foreach (var eventStore in await storage.GetEventStores())
        {
            foreach (var ns in await storage.GetEventStore(eventStore).Namespaces.GetAll())
            {
                logger.LegacyFences(await records.CountLegacyFences(eventStore, ns.Name));
            }
        }
    }

    /// <summary>Report the actual guarantees and the release-N warning for each unsupported member.</summary>
    internal void ReportCapabilities()
    {
        foreach (var capability in keys.Capabilities)
        {
            logger.StorageCapability(capability.Type, capability.Guarantees);
            if (capability.Guarantees == EncryptionKeyFenceGuarantees.None) logger.UnsupportedStorage(capability.Type);
        }
    }

    async Task Start(CancellationToken cancellationToken)
    {
        ProtectedValues.ReleaseKeys.Configure(options.Value.Compliance.Encryption.RevocationGrace);
        logger.RolloutWarning();
        logger.RevocationGrace(options.Value.Compliance.Encryption.RevocationGrace);
        ReportCapabilities();
        await PublishSafely();
        _heartbeats = Heartbeat();
    }

    async Task Stop(CancellationToken cancellationToken)
    {
        await _stop.CancelAsync();
        await _heartbeats;
    }

    async Task Heartbeat()
    {
        using var timer = new PeriodicTimer(membership.Value.IAmAliveTablePublishTimeout, timeProvider);
        try
        {
            while (await timer.WaitForNextTickAsync(_stop.Token)) await PublishSafely();
        }
        catch (OperationCanceledException) when (_stop.IsCancellationRequested)
        {
            // Silo shutdown ends this incarnation's heartbeat, without touching other silo records.
        }
    }

    async Task PublishSafely()
    {
        try
        {
            await Publish();
        }
        catch (Exception)
        {
            // Do not log provider exception messages: they may contain subject identifiers or secret names.
            logger.ProtocolPublicationFailed();
        }
    }
}
