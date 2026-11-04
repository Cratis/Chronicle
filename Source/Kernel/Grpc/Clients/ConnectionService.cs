// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reactive.Linq;
using System.Reactive.Subjects;
using Cratis.Chronicle.Clients;
using Cratis.Chronicle.Compatibility;
using Cratis.Chronicle.Configuration;
using Cratis.Chronicle.Contracts.Clients;
using Cratis.Chronicle.Services.Host;
using Grpc.Core;
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
/// <param name="openConnections"><see cref="OpenConnections"/> tracking the connections open on this silo.</param>
/// <param name="logger"><see cref="ILogger"/> for logging.</param>
/// <param name="options"><see cref="IOptions{ChronicleOptions}"/> for configuration.</param>
/// <param name="ownership">Local connection ownership.</param>
/// <param name="owners">The caller's principal resolver.</param>
/// <param name="visibility">The connection metadata visibility filter.</param>
internal sealed class ConnectionService(
    IGrainFactory grainFactory,
    ILocalSiloDetails localSiloDetails,
    ConnectedClientsQuery connectedClientsQuery,
    OpenConnections openConnections,
    ILogger<ConnectionService> logger,
    IOptions<ChronicleOptions> options,
    IConnectionOwnership ownership,
    IConnectionOwnerResolver owners,
    IConnectedClientsVisibility visibility) : IConnectionService
{
    static readonly Lazy<string> _schemaDefinition = new(GenerateSchema);
    static readonly Lazy<WireContract> _wireContract = new(() => WireContractReader.Read(Contracts.WireContractDescriptorSet.Bytes));
    readonly TimeSpan _keepAliveInterval = TimeSpan.FromSeconds(options.Value.ConnectedClients.KeepAliveIntervalSeconds);

    /// <summary>
    /// The longest gap between responses from a client that cannot have got it dropped as stale.
    /// </summary>
    /// <remarks>
    /// The registry drops a client whose last response is older than the stale threshold when it checks. A response
    /// reaches the registry a moment after it reaches this service, so a gap within one keep-alive of the threshold is
    /// already treated as one that may have got the client dropped.
    /// </remarks>
    readonly TimeSpan _lapse = TimeSpan.FromSeconds(Math.Max(
        options.Value.ConnectedClients.StaleThresholdSeconds - options.Value.ConnectedClients.KeepAliveIntervalSeconds,
        options.Value.ConnectedClients.KeepAliveIntervalSeconds));

    /// <inheritdoc/>
    public IObservable<ConnectionKeepAlive> Connect(
        ConnectRequest request,
        CallContext context = default)
    {
        var owner = owners.Resolve(context.ServerCallContext?.GetHttpContext().User, context.ServerCallContext is not null);

        // Replaying, so the first keep-alive is not lost. It is sent as soon as the client is registered, which
        // races the transport subscribing to this subject - and a plain Subject drops what it has no subscriber
        // for, which would silently put the client back to waiting a full interval. One is all that needs
        // replaying: a keep-alive carries nothing but the connection it belongs to.
        var subject = new ReplaySubject<ConnectionKeepAlive>(1);
        _ = Task.Run(
            async () =>
            {
                ConnectionClaim? claim = null;
                IConnectedClients? connectedClients = null;
                Exception? failure = null;
                try
                {
                    using (await ownership.Enter(request.ConnectionId, context.CancellationToken))
                    {
                        context.CancellationToken.ThrowIfCancellationRequested();
                        claim = ownership.Claim(request.ConnectionId, owner);
                        if (claim is null)
                        {
                            logger.ConnectionRejected(request.ConnectionId);
                            throw new RpcException(new Status(StatusCode.PermissionDenied, "The connection belongs to another caller."));
                        }

                        connectedClients = grainFactory.GetConnectedClients(localSiloDetails.SiloAddress);
                        await Register(connectedClients, request);
                        context.CancellationToken.ThrowIfCancellationRequested();
                        openConnections.Opened(request, DateTimeOffset.UtcNow);
                    }

                    // The session is established the moment the client is registered, and the first keep-alive is
                    // how the client learns that - it does not treat itself as connected until one arrives. Send it
                    // straight away rather than after a first interval, or every client everywhere pays the whole
                    // keep-alive interval before it can do anything.
                    subject.OnNext(new ConnectionKeepAlive
                    {
                        ConnectionId = request.ConnectionId
                    });

                    while (!context.CancellationToken.IsCancellationRequested)
                    {
                        await Task.Delay(_keepAliveInterval, context.CancellationToken).ConfigureAwait(false);
                        using (await ownership.Enter(request.ConnectionId, context.CancellationToken))
                        {
                            context.CancellationToken.ThrowIfCancellationRequested();
                            if (!ownership.IsCurrent(request.ConnectionId, claim)) break;

                            if (request.IsRunningWithDebugger)
                            {
                                await KeepRegistered(connectedClients, request);
                            }
                            else if (openConnections.HasLapsed(request.ConnectionId))
                            {
                                break;
                            }

                            subject.OnNext(new ConnectionKeepAlive
                            {
                                ConnectionId = request.ConnectionId
                            });
                        }
                    }
                }
                catch (OperationCanceledException) when (context.CancellationToken.IsCancellationRequested)
                {
                }
                catch (Exception ex)
                {
                    logger.FailureDuringKeepAlive(request.ConnectionId, ex);
                    failure = ex;
                }
                finally
                {
                    try
                    {
                        if (claim is not null)
                        {
                            using (await ownership.Enter(request.ConnectionId))
                            {
                                if (ownership.IsCurrent(request.ConnectionId, claim))
                                {
                                    try
                                    {
                                        openConnections.Closed(request.ConnectionId);
                                        if (connectedClients is not null)
                                        {
                                            await connectedClients.OnClientDisconnected(request.ConnectionId, "Client disconnected");
                                        }
                                    }
                                    finally
                                    {
                                        ownership.Release(request.ConnectionId, claim);
                                    }
                                }
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        logger.FailureDuringKeepAlive(request.ConnectionId, ex);
                        failure ??= ex;
                    }

                    if (failure is not null) subject.OnError(failure);
                    else subject.OnCompleted();
                }
            });

        return subject;
    }

    /// <inheritdoc/>
    public async Task ConnectionKeepAlive(ConnectionKeepAlive keepAlive)
    {
        var owner = owners.Current();
        using var gate = await ownership.Enter(keepAlive.ConnectionId);
        var access = ownership.Authorize(keepAlive.ConnectionId, owner);
        if (access == ConnectionAccess.Denied)
        {
            logger.ConnectionRejected(keepAlive.ConnectionId);
            throw new RpcException(new Status(StatusCode.PermissionDenied, "The connection belongs to another caller."));
        }

        if (access == ConnectionAccess.Unknown) return;
        var respondedAt = DateTimeOffset.UtcNow;
        try
        {
            var connectedClients = grainFactory.GetConnectedClients(localSiloDetails.SiloAddress);
            var registryKnowsClient = await connectedClients.OnClientPing(keepAlive.ConnectionId);

            if (openConnections.Responded(keepAlive.ConnectionId, registryKnowsClient, respondedAt, _lapse, out var request))
            {
                await Restore(connectedClients, request);
            }
        }
        finally
        {
            openConnections.Handled(keepAlive.ConnectionId, DateTimeOffset.UtcNow);
        }
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
    /// <para>
    /// Ordered by protocol version rather than always treating the caller as the older side - see
    /// <see cref="ConnectCompatibility"/> for why that distinction matters and what happens when it is
    /// skipped (#4058).
    /// </para>
    /// </remarks>
    [AllowAnonymous]
    public Task<CompatibilityResponse> CheckCompatibility(CompatibilityRequest request)
    {
        var response = new CompatibilityResponse
        {
            ServerVersion = ServerVersion.Version,
            ServerProtocolVersion = Contracts.ProtocolVersion.Current,
            Capabilities = [KernelCapabilities.PreciseEventTypeSchemas, KernelCapabilities.ClosesStreamConstraints, KernelCapabilities.EffectReceipts, KernelCapabilities.Occurrences, KernelCapabilities.ConcurrencyScopeConjunctions, KernelCapabilities.IdempotentAppends, KernelCapabilities.CompositeKeyLookups]
        };

        try
        {
            var report = ConnectCompatibility.Check(
                WireContractReader.Read(request.DescriptorSet),
                request.ProtocolVersion,
                _wireContract.Value,
                response.ServerProtocolVersion);

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
    public async Task<IEnumerable<ConnectedClient>> GetConnectedClients(CallContext context = default)
    {
        var filter = visibility.Capture(context.ServerCallContext?.GetHttpContext().User, context.ServerCallContext is not null);
        return await filter(await connectedClientsQuery.GetAll());
    }

    /// <inheritdoc/>
    public IObservable<IEnumerable<ConnectedClient>> ObserveConnectedClients(CallContext context = default)
    {
        var filter = visibility.Capture(context.ServerCallContext?.GetHttpContext().User, context.ServerCallContext is not null);
        return connectedClientsQuery.ObserveAll(context.CancellationToken)
            .Select(clients => Observable.FromAsync(() => filter(clients))).Concat();
    }

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

    static Task Register(IConnectedClients connectedClients, ConnectRequest request) =>
        connectedClients.OnClientConnected(
            request.ConnectionId,
            request.ClientVersion,
            request.IsRunningWithDebugger,
            request.ProcessId,
            request.ProcessPath,
            request.MachineName,
            request.ClientType);

    /// <summary>
    /// Registers a client running with a debugger again if its silo's registry no longer knows it while the connection
    /// is still open.
    /// </summary>
    /// <remarks>
    /// The registry lives in a grain activation only. Orleans deactivates an activation when the silo owning its
    /// directory entry leaves the cluster, and the new activation starts out knowing no client - so a client
    /// connected to a surviving silo would otherwise be treated as gone by every observer it is subscribed to.
    /// <para>
    /// Every other client is put back only when it responds to a keep-alive - see <see cref="ConnectionKeepAlive"/>.
    /// A client running with a debugger does not respond, and is never dropped for not responding either, so the
    /// registry not knowing it can only mean the registry was lost.
    /// </para>
    /// </remarks>
    /// <param name="connectedClients">The <see cref="IConnectedClients"/> registry of the silo the client is connected to.</param>
    /// <param name="request">The <see cref="ConnectRequest"/> the client connected with.</param>
    /// <returns>Awaitable task.</returns>
    async Task KeepRegistered(IConnectedClients connectedClients, ConnectRequest request)
    {
        try
        {
            if (await connectedClients.IsConnected(request.ConnectionId))
            {
                return;
            }

            await Restore(connectedClients, request);
        }
        catch (Exception ex)
        {
            // A silo that is still leaving the cluster can fail a call or two; the next keep-alive retries.
            logger.FailedToKeepClientRegistered(request.ConnectionId, ex);
        }
    }

    async Task Restore(IConnectedClients connectedClients, ConnectRequest request)
    {
        try
        {
            logger.RegisteringClientAgain(request.ConnectionId);
            await connectedClients.OnClientRestored(
                request.ConnectionId,
                request.ClientVersion,
                request.IsRunningWithDebugger,
                request.ProcessId,
                request.ProcessPath,
                request.MachineName,
                request.ClientType);
        }
        catch (Exception ex)
        {
            // A silo that is still leaving the cluster can fail a call or two; the next keep-alive retries.
            logger.FailedToKeepClientRegistered(request.ConnectionId, ex);
        }
    }
}
