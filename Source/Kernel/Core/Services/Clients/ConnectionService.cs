// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reactive.Linq;
using System.Reactive.Subjects;
using Cratis.Chronicle.Clients;
using Cratis.Chronicle.Compatibility;
using Cratis.Chronicle.Configuration;
using Cratis.Chronicle.Contracts.Clients;
using Cratis.Chronicle.Services.Host;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ProtoBuf.Grpc;
using ProtoBuf.Grpc.Reflection;
using ProtoBuf.Meta;

namespace Cratis.Chronicle.Services.Clients;

/// <summary>
/// Represents an implementation of <see cref="IConnectionService"/>.
/// </summary>
/// <param name="grainFactory"><see cref="IGrainFactory"/> to get grains with.</param>
/// <param name="localSiloDetails"><see cref="ILocalSiloDetails"/> for the silo terminating the client connections.</param>
/// <param name="connectedClientsQuery"><see cref="ConnectedClientsQuery"/> for the cluster-wide view of connected clients.</param>
/// <param name="logger"><see cref="ILogger"/> for logging.</param>
/// <param name="options"><see cref="IOptions{ChronicleOptions}"/> for configuration.</param>
internal sealed class ConnectionService(
    IGrainFactory grainFactory,
    ILocalSiloDetails localSiloDetails,
    ConnectedClientsQuery connectedClientsQuery,
    ILogger<ConnectionService> logger,
    IOptions<ChronicleOptions> options) : IConnectionService
{
    static readonly Lazy<string> _schemaDefinition = new(GenerateSchema);
    static readonly Lazy<WireContract> _wireContract = new(() => WireContractReader.Read(Contracts.WireContractDescriptorSet.Bytes));
    readonly TimeSpan _keepAliveInterval = TimeSpan.FromSeconds(options.Value.ConnectedClients.KeepAliveIntervalSeconds);

    /// <inheritdoc/>
    public IObservable<ConnectionKeepAlive> Connect(
        ConnectRequest request,
        CallContext context = default)
    {
        var subject = new Subject<ConnectionKeepAlive>();
        var connectedClients = grainFactory.GetConnectedClients(localSiloDetails.SiloAddress);

        _ = Task.Run(
            async () =>
            {
                await connectedClients.OnClientConnected(
                    request.ConnectionId,
                    request.ClientVersion,
                    request.IsRunningWithDebugger,
                    request.ProcessId,
                    request.ProcessPath,
                    request.MachineName,
                    request.ClientType);

                try
                {
                    while (!context.CancellationToken.IsCancellationRequested)
                    {
                        await Task.Delay(_keepAliveInterval).ConfigureAwait(false);

                        if (context.CancellationToken.IsCancellationRequested)
                        {
                            break;
                        }

                        subject.OnNext(new ConnectionKeepAlive
                        {
                            ConnectionId = request.ConnectionId
                        });
                    }
                }
                catch (Exception ex)
                {
                    logger.FailureDuringKeepAlive(request.ConnectionId, ex);
                }

                await connectedClients.OnClientDisconnected(request.ConnectionId, "Client disconnected");
            },
            context.CancellationToken);

        context.CancellationToken.Register(() =>
        {
            subject.OnCompleted();
            subject.Dispose();
        });

        return subject;
    }

    /// <inheritdoc/>
    public async Task ConnectionKeepAlive(ConnectionKeepAlive keepAlive)
    {
        var connectedClients = grainFactory.GetConnectedClients(localSiloDetails.SiloAddress);
        await connectedClients.OnClientPing(keepAlive.ConnectionId);
    }

    /// <inheritdoc/>
    [AllowAnonymous]
    public Task<DescriptorSetResponse> GetDescriptorSet()
    {
        return Task.FromResult(new DescriptorSetResponse
        {
            SchemaDefinition = _schemaDefinition.Value
        });
    }

    /// <inheritdoc/>
    /// <remarks>
    /// Anonymous, because a client that cannot talk to this server should be told so plainly rather than being
    /// turned away as unauthenticated - the whole point is to name the mismatch. The check reads nothing but the
    /// descriptor set the caller sent and the one this server ships.
    /// </remarks>
    [AllowAnonymous]
    public Task<CompatibilityResponse> CheckCompatibility(CompatibilityRequest request)
    {
        var response = new CompatibilityResponse
        {
            ServerVersion = ServerVersion.Version,
            ServerProtocolVersion = Contracts.ProtocolVersion.Current
        };

        try
        {
            var report = WireCompatibilityChecker.Check(
                WireContractReader.Read(request.DescriptorSet),
                _wireContract.Value);

            response.IsCompatible = report.IsCompatible;
            response.Incompatibilities = [.. report.Incompatibilities.Select(_ => _.ToString())];

            if (!report.IsCompatible)
            {
                logger.ClientIsIncompatible(request.ClientType, request.ClientVersion, request.ProtocolVersion, report.Incompatibilities.Count);
            }
        }
        catch (Exception ex)
        {
            // A descriptor set that will not parse says nothing about whether the two sides agree, and refusing the
            // connection over it would turn a malformed payload from one client into an outage for it. Report it as
            // an incompatibility so the client can say something useful, and let the client decide.
            logger.FailedToReadClientDescriptorSet(request.ClientType, ex);
            response.IsCompatible = false;
            response.Incompatibilities = [$"The descriptor set the client sent could not be read: {ex.Message}"];
        }

        return Task.FromResult(response);
    }

    /// <inheritdoc/>
    public Task<IEnumerable<ConnectedClient>> GetConnectedClients(CallContext context = default) =>
        connectedClientsQuery.GetAll();

    /// <inheritdoc/>
    public IObservable<IEnumerable<ConnectedClient>> ObserveConnectedClients(CallContext context = default) =>
        connectedClientsQuery.ObserveAll(context.CancellationToken);

    static string GenerateSchema()
    {
        var generator = new SchemaGenerator
        {
            ProtoSyntax = ProtoSyntax.Proto3
        };

        // SchemaGenerator requires all types in a single call to share the same proto package
        // (derived from C# namespace). Group by namespace and concatenate the resulting schemas.
        var schemas = Contracts.AvailableServices.All
            .GroupBy(t => t.Namespace ?? string.Empty)
            .Select(group => generator.GetSchema(group.ToArray()));

        return string.Join('\n', schemas);
    }
}
