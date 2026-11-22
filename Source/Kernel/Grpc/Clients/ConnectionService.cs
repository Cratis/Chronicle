// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reactive.Linq;
using System.Reactive.Subjects;
using Cratis.Chronicle.Clients;
using Cratis.Chronicle.Compatibility;
using Cratis.Chronicle.Configuration;
using Cratis.Chronicle.Contracts.Clients;
using Cratis.Chronicle.Diagnostics.Load;
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
/// <param name="ownership"><see cref="IConnectionOwnership"/> telling which caller opened each connection.</param>
/// <param name="visibility"><see cref="IConnectedClientsVisibility"/> limiting which connected clients a caller sees.</param>
/// <param name="logger"><see cref="ILogger"/> for logging.</param>
/// <param name="options"><see cref="IOptions{ChronicleOptions}"/> for configuration.</param>
/// <param name="kernelLoad">The <see cref="IKernelLoad"/> telling how busy this silo is, reported on every keep-alive.</param>
internal sealed class ConnectionService(
    IGrainFactory grainFactory,
    ILocalSiloDetails localSiloDetails,
    ConnectedClientsQuery connectedClientsQuery,
    OpenConnections openConnections,
    IConnectionOwnership ownership,
    IConnectedClientsVisibility visibility,
    ILogger<ConnectionService> logger,
    IOptions<ChronicleOptions> options,
    IKernelLoad? kernelLoad = null) : IConnectionService
{
    /// <summary>
    /// How many keep-alive intervals a reservation lease lasts without being renewed. The holder renews it every
    /// half lease, off the keep-alive path, so a lease survives a few missed or slow renewals but a reservation nobody holds any more
    /// - a lost release, a holder that died - lapses within a few intervals.
    /// </summary>
    const int ReservationLeaseIntervals = 4;

    static readonly Status _notAvailableToCaller = new(StatusCode.PermissionDenied, "The connection is not available to the caller.");
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

    /// <summary>
    /// Gets or sets the clock used for registration deadlines.
    /// </summary>
    internal TimeProvider RegistrationTimeProvider { get; set; } = TimeProvider.System;

    /// <summary>
    /// How long a connect or a keep-alive waits for the grains behind it before answering anyway.
    /// </summary>
    TimeSpan Grace => TimeSpan.FromSeconds(options.Value.ConnectedClients.GraceSeconds);

    /// <summary>
    /// How often the holder renews its lease: half of the lease, so a lease survives a missed renewal.
    /// </summary>
    TimeSpan RenewalPeriod => _keepAliveInterval * ReservationLeaseIntervals / 2;

    /// <summary>
    /// How long a renewal may wait for the reservation: never longer than the period, so one renewal is over before the next is due.
    /// </summary>
    TimeSpan RenewalTimeout => options.Value.Clustering.ResponseTimeout < RenewalPeriod ? options.Value.Clustering.ResponseTimeout : RenewalPeriod;

    /// <inheritdoc/>
    public IObservable<ConnectionKeepAlive> Connect(
        ConnectRequest request,
        CallContext context = default)
    {
        // Replaying, so the first keep-alive is not lost. It is sent as soon as the client is registered, which
        // races the transport subscribing to this subject - and a plain Subject drops what it has no subscriber
        // for, which would silently put the client back to waiting a full interval. One is all that needs
        // replaying: a keep-alive carries nothing but the connection it belongs to.
        var subject = new ReplaySubject<ConnectionKeepAlive>(1);
        var caller = ConnectionCallers.From(context, options.Value);
        var connectedClients = grainFactory.GetConnectedClients(localSiloDetails.SiloAddress);

        // The producer is the only owner of the subject: it alone emits and terminates, so a cancelling call and a
        // registration finishing cannot race each other into two terminal notifications.
        _ = Task.Run(() => Produce(subject, connectedClients, request, caller.Owner, context.CancellationToken), CancellationToken.None);

        return subject;
    }

    /// <inheritdoc/>
    public async Task ConnectionKeepAlive(ConnectionKeepAlive keepAlive, CallContext context = default)
    {
        var caller = ConnectionCallers.From(context, options.Value);
        var verification = ownership.Verify(grainFactory, keepAlive.ConnectionId, caller.Owner, options.Value.Clustering.ResponseTimeout, timeProvider: RegistrationTimeProvider, cancellationToken: context.CancellationToken);
#pragma warning disable MA0042 // Result is read only after successful synchronous completion; the hot path must not block or allocate.
        var access = verification.IsCompletedSuccessfully ? verification.Result : await verification;
#pragma warning restore MA0042
        if (access == ConnectionAccessOutcome.Unverifiable)
        {
            logger.KeepAliveOwnershipUnverifiable(keepAlive.ConnectionId);
            throw new RpcException(new Status(StatusCode.Unavailable, "The connection could not be verified."));
        }
        if (access == ConnectionAccessOutcome.Denied)
        {
            logger.KeepAliveRefused(keepAlive.ConnectionId);
            throw new RpcException(_notAvailableToCaller);
        }

        // The registry is bookkeeping: the client is answered within the grace whatever the grain behind it is doing, and
        // the response is applied to the registry whenever the registry gets to it. The bookkeeping never holds the answer back.
        var handling = HandleResponse(keepAlive.ConnectionId, DateTimeOffset.UtcNow);
        try
        {
            await handling.WaitAsync(Grace, RegistrationTimeProvider, context.CancellationToken).ConfigureAwait(false);
        }
        catch (TimeoutException)
        {
            logger.KeepAliveBookkeepingContinuesInBackground(keepAlive.ConnectionId);
            _ = handling.ContinueWith(
                task => logger.FailedToKeepClientRegistered(keepAlive.ConnectionId, task.Exception!),
                CancellationToken.None,
                TaskContinuationOptions.OnlyOnFaulted | TaskContinuationOptions.ExecuteSynchronously,
                TaskScheduler.Default);
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
            Capabilities = [KernelCapabilities.PreciseEventTypeSchemas, KernelCapabilities.ClosesStreamConstraints, KernelCapabilities.EffectReceipts, KernelCapabilities.Occurrences, KernelCapabilities.Imports, KernelCapabilities.ConcurrencyScopeConjunctions, KernelCapabilities.IdempotentAppends, KernelCapabilities.CompositeKeyLookups, KernelCapabilities.MultiplexedObservation]
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
        var caller = ConnectionCallers.From(context, options.Value);
        return await visibility.Filter(await connectedClientsQuery.GetAll(), caller);
    }

    /// <inheritdoc/>
    public IObservable<IEnumerable<ConnectedClient>> ObserveConnectedClients(CallContext context = default)
    {
        var caller = ConnectionCallers.From(context, options.Value);
        return connectedClientsQuery
            .ObserveAll(context.CancellationToken)
            .Select(clients => Observable.FromAsync(() => visibility.Filter(clients, caller)))
            .Concat();
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

    static async Task EndRenewal(CancellationTokenSource renewals, Task<ReservationOutcome>? renewal)
    {
        await renewals.CancelAsync().ConfigureAwait(false);
        if (renewal is null) return;

        try
        {
            await renewal.ConfigureAwait(false);
        }
        catch
        {
            // The renewal reports its own failures; ending the stream must not depend on it.
        }
    }

    async Task PingOwningSilo(Cratis.Chronicle.Concepts.Clients.ConnectionId connectionId)
    {
        var owner = await ConnectedClientsLocator.Find(grainFactory, localSiloDetails.SiloAddress, connectionId);
        if (owner is not null)
        {
            await owner.OnClientPing(connectionId);
        }
    }

    async Task Produce(
        ReplaySubject<ConnectionKeepAlive> subject,
        IConnectedClients connectedClients,
        ConnectRequest request,
        ConnectionOwner owner,
        CancellationToken cancellationToken)
    {
        var token = Guid.NewGuid();
        if (owner.IsUnidentified)
        {
            logger.ConnectionRefusedWithoutIdentity(request.ConnectionId);
            subject.OnError(new RpcException(_notAvailableToCaller));
            return;
        }

        try
        {
            await Serve(subject, connectedClients, request, owner, token, cancellationToken);
        }
        finally
        {
            ReleaseReservation(request.ConnectionId, owner, token);
        }
    }

    /// <summary>
    /// Verifies across the cluster that no other caller holds the connection, and takes the cluster-wide reservation.
    /// </summary>
    /// <remarks>
    /// Both steps are grain calls, so this never blocks the connection: the caller waits for it for a short grace and
    /// carries on if the grains are slow, and the outcome is acted on whenever it arrives. It reports its failures
    /// rather than throwing them.
    /// </remarks>
    /// <param name="request">The <see cref="ConnectRequest"/> the client connected with.</param>
    /// <param name="owner">The <see cref="ConnectionOwner"/> opening the connection.</param>
    /// <param name="token">The token identifying this connection's claim on the reservation.</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/> that ends the verification.</param>
    /// <returns>Granted when verified, Refused when another caller holds the connection, Failed when it could not be told.</returns>
    async Task<ReservationOutcome> VerifyAcrossCluster(ConnectRequest request, ConnectionOwner owner, Guid token, CancellationToken cancellationToken)
    {
        try
        {
            var isElsewhere = await connectedClientsQuery
                .IsConnectedOnAnotherSilo(localSiloDetails.SiloAddress, request.ConnectionId)
                .WaitAsync(options.Value.Clustering.ResponseTimeout, RegistrationTimeProvider, cancellationToken)
                .ConfigureAwait(false);

            if (isElsewhere)
            {
                logger.ConnectionRefusedForAnotherCaller(request.ConnectionId);
                return ReservationOutcome.Refused;
            }

            var reservation = await TryReserve(request, owner, token, cancellationToken);
            if (reservation == ReservationOutcome.Refused)
            {
                logger.ConnectionRefusedForAnotherCaller(request.ConnectionId);
            }

            return reservation;
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            return ReservationOutcome.Failed;
        }
        catch (Exception ex)
        {
            logger.ClientRegistrationFailed(request.ConnectionId, ex);
            return ReservationOutcome.Failed;
        }
    }

    async Task<ReservationOutcome> TryReserve(ConnectRequest request, ConnectionOwner owner, Guid token, CancellationToken cancellationToken)
    {
        try
        {
            return await ClaimReservation(request.ConnectionId, owner, token, options.Value.Clustering.ResponseTimeout, cancellationToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            // The claim may still be processed after it was given up on, so it has to be undone.
            ReleaseReservation(request.ConnectionId, owner, token);
            return ReservationOutcome.Failed;
        }
        catch (Exception ex)
        {
            logger.ConnectionReservationFailed(request.ConnectionId, ex);
            ReleaseReservation(request.ConnectionId, owner, token);
            return ReservationOutcome.Failed;
        }
    }

    async Task<ReservationOutcome> RenewReservation(ConnectRequest request, ConnectionOwner owner, Guid token, CancellationToken cancellationToken)
    {
        try
        {
            return await ClaimReservation(request.ConnectionId, owner, token, RenewalTimeout, cancellationToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            return ReservationOutcome.Failed;
        }
        catch (Exception ex)
        {
            // The lease outlasts a few missed renewals, so this is tried again at the next renewal period.
            logger.FailedToRenewReservation(request.ConnectionId, ex);
            return ReservationOutcome.Failed;
        }
    }

    async Task<ReservationOutcome> ClaimReservation(string connectionId, ConnectionOwner owner, Guid token, TimeSpan timeout, CancellationToken cancellationToken)
    {
        var granted = await grainFactory
            .GetGrain<IConnectionReservation>(connectionId)
            .Claim(owner, localSiloDetails.SiloAddress, token, _keepAliveInterval * ReservationLeaseIntervals)
            .WaitAsync(timeout, RegistrationTimeProvider, cancellationToken)
            .ConfigureAwait(false);
        return granted ? ReservationOutcome.Granted : ReservationOutcome.Refused;
    }

    void ReleaseReservation(string connectionId, ConnectionOwner owner, Guid token)
    {
        var reservation = grainFactory.GetGrain<IConnectionReservation>(connectionId);
        _ = ReleaseReservation(reservation, connectionId, owner, token);
    }

    async Task ReleaseReservation(IConnectionReservation reservation, string connectionId, ConnectionOwner owner, Guid token)
    {
        try
        {
            await reservation
                .Release(owner, token)
                .WaitAsync(options.Value.Clustering.ResponseTimeout, RegistrationTimeProvider, CancellationToken.None)
                .ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            logger.FailedToReleaseReservation(connectionId, ex);
        }
    }

    async Task Serve(
        ReplaySubject<ConnectionKeepAlive> subject,
        IConnectedClients connectedClients,
        ConnectRequest request,
        ConnectionOwner owner,
        Guid token,
        CancellationToken cancellationToken)
    {
        var terminated = false;
        var reservedByAnother = false;
        var claimed = false;
        try
        {
            using var renewals = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);

            // The cluster-wide checks are grain calls, so they must not decide whether the client is answered: the
            // connection is served once the local claim holds, the checks carry on, and a refusal that arrives later ends the stream.
            var verification = VerifyAcrossCluster(request, owner, token, renewals.Token);
            var (proceed, outcome) = await TryVerify(subject, verification, cancellationToken);
            if (!proceed)
            {
                return;
            }

            if (ownership.Claim(request.ConnectionId, owner) == ConnectionClaim.Refused)
            {
                logger.ConnectionRefusedForAnotherCaller(request.ConnectionId);
                subject.OnError(new RpcException(_notAvailableToCaller));
                return;
            }

            claimed = true;
            var (registered, registration) = await TryRegister(subject, connectedClients, request, cancellationToken);
            if (!registered)
            {
                return;
            }

            openConnections.Opened(request, DateTimeOffset.UtcNow);
            var keepRegistered = Task.CompletedTask;
            var renewal = outcome is null ? verification : null;
            var retryVerification = outcome == ReservationOutcome.Failed;
            var lastRenewal = RegistrationTimeProvider.GetTimestamp();

            try
            {
                // The session is established the moment the client is registered, and the first keep-alive is
                // how the client learns that - it does not treat itself as connected until one arrives. Send it
                // straight away rather than after a first interval, or every client everywhere pays the whole
                // keep-alive interval before it can do anything.
                subject.OnNext(AliveFor(request.ConnectionId));

                while (!cancellationToken.IsCancellationRequested)
                {
#pragma warning disable MA0166 // The registration clock seam must not change existing heartbeat timing
                    await Task.Delay(_keepAliveInterval, cancellationToken).ConfigureAwait(false);
#pragma warning restore MA0166

                    if (request.IsRunningWithDebugger)
                    {
                        // Only the wait is cancellable and bounded: an ending call must not wait out a registry call, and a
                        // registry too slow to answer must not hold the next keep-alive back. KeepRegistered handles its own
                        // failures, so a check given up on cannot fault unobserved.
                        keepRegistered = KeepRegistered(connectedClients, request, cancellationToken);
                        await WaitForGrace(keepRegistered, cancellationToken).ConfigureAwait(false);
                    }
                    else if (openConnections.HasLapsed(request.ConnectionId))
                    {
                        break;
                    }

                    // A renewal never holds back a keep-alive: it runs on its own, at most one at a time, and only
                    // a finished renewal that was refused closes the stream.
                    if (renewal is { IsCompleted: true })
                    {
                        var renewed = await renewal.ConfigureAwait(false);
                        renewal = null;
                        if (renewed == ReservationOutcome.Refused)
                        {
                            logger.ConnectionClosedForAnotherCaller(request.ConnectionId);
                            reservedByAnother = true;
                            break;
                        }

                        // A connection that could not be verified when it opened is verified again at the next keep-alive.
                        if (renewed == ReservationOutcome.Granted)
                        {
                            outcome = ReservationOutcome.Granted;
                        }
                        else if (outcome != ReservationOutcome.Granted)
                        {
                            retryVerification = true;
                        }
                    }

                    if (renewal is null && (retryVerification || RegistrationTimeProvider.GetElapsedTime(lastRenewal) >= RenewalPeriod))
                    {
                        lastRenewal = RegistrationTimeProvider.GetTimestamp();
                        retryVerification = false;
                        renewal = outcome == ReservationOutcome.Granted
                            ? RenewReservation(request, owner, token, renewals.Token)
                            : VerifyAcrossCluster(request, owner, token, renewals.Token);
                    }

                    subject.OnNext(AliveFor(request.ConnectionId));
                }
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                // The call ended while waiting for the next keep-alive.
            }
            catch (Exception ex)
            {
                logger.FailureDuringKeepAlive(request.ConnectionId, ex);
            }
            finally
            {
                // A renewal still pending must be over before the reservation is released, or it could claim it again.
                await EndRenewal(renewals, renewal).ConfigureAwait(false);
            }

            terminated = true;
            if (reservedByAnother)
            {
                // The connection was served before the cluster said it was not this caller's. The claim must not be kept
                // as a released one, or it would lock out the connection's real owner on this silo.
                ownership.Revoke(request.ConnectionId, owner);
                claimed = false;
                subject.OnError(new RpcException(_notAvailableToCaller));
            }
            else
            {
                subject.OnCompleted();
            }

            openConnections.Closed(request.ConnectionId);

            // A registry check given up on above may still be putting the client back. Let it settle first, so the
            // disconnect is the registry's last word for this connection rather than being undone by a late restore.
            // The same goes for a registration still running after the grace: it is the registry's last word too.
            await WaitForSettling(keepRegistered).ConfigureAwait(false);
            await WaitForSettling(registration).ConfigureAwait(false);
            await connectedClients.OnClientDisconnected(request.ConnectionId, "Client disconnected");
        }
        catch (Exception ex)
        {
            ReportConnectionFailure(subject, request.ConnectionId, terminated, ex);
        }
        finally
        {
            if (claimed)
            {
                ownership.Release(request.ConnectionId, owner);
            }
        }
    }

    void ReportConnectionFailure(ReplaySubject<ConnectionKeepAlive> subject, string connectionId, bool terminated, Exception exception)
    {
        logger.ConnectionFailed(connectionId, exception);
        if (!terminated)
        {
            // Nothing is left to run this connection, so the client must be told rather than left waiting.
            subject.OnError(new RpcException(new Status(StatusCode.Unknown, "Connection failed")));
        }
    }

    /// <summary>
    /// Waits a short grace for the cluster-wide verification, and goes on without it if the grains are slow.
    /// </summary>
    /// <param name="subject">The <see cref="ReplaySubject{T}"/> to terminate when the connection is refused or the call is cancelled.</param>
    /// <param name="verification">The running verification.</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/> of the call.</param>
    /// <returns>Whether to go on, and the outcome of the verification, or null when it is still running.</returns>
    async Task<(bool Proceed, ReservationOutcome? Outcome)> TryVerify(
        ReplaySubject<ConnectionKeepAlive> subject,
        Task<ReservationOutcome> verification,
        CancellationToken cancellationToken)
    {
        ReservationOutcome? outcome = null;
        try
        {
            outcome = await verification.WaitAsync(Grace, RegistrationTimeProvider, cancellationToken).ConfigureAwait(false);
        }
        catch (TimeoutException)
        {
            // The grains are slow. The connection is served and the verification is acted on when it arrives.
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            // The call ended while verifying - not an error.
        }

        if (cancellationToken.IsCancellationRequested)
        {
            subject.OnCompleted();
            return (false, null);
        }

        if (outcome == ReservationOutcome.Refused)
        {
            subject.OnError(new RpcException(_notAvailableToCaller));
            return (false, outcome);
        }

        return (true, outcome);
    }

    /// <summary>
    /// Registers the client, reporting a failed or cancelled registration to the subject, and going on without it when
    /// the registry is slow.
    /// </summary>
    /// <remarks>
    /// The registry is bookkeeping, so a registry that has not answered within the grace does not hold the client
    /// back: the registration carries on, and a client the registry does not know when it responds to a keep-alive is registered again.
    /// </remarks>
    /// <param name="subject">The <see cref="ReplaySubject{T}"/> to terminate when the client is not registered.</param>
    /// <param name="connectedClients">The <see cref="IConnectedClients"/> registry of the silo the client is connected to.</param>
    /// <param name="request">The <see cref="ConnectRequest"/> the client connected with.</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/> of the call.</param>
    /// <returns>Whether the call is live and the client is served, and the registration, which may still be running.</returns>
    async Task<(bool Proceed, Task Registration)> TryRegister(
        ReplaySubject<ConnectionKeepAlive> subject,
        IConnectedClients connectedClients,
        ConnectRequest request,
        CancellationToken cancellationToken)
    {
        var pending = Task.CompletedTask;
        if (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                var registration = Register(connectedClients, request);
                pending = registration;

                // Only the wait is bounded and cancellable, not the registration itself. Should it fault after the
                // wait has been given up on, nobody awaits it - observe it so the fault is not unobserved.
                //
                // A registration given up on (cancelled) is deliberately not compensated with a disconnect at once.
                // The client retries an uncompleted connect with the same ConnectionId and the registry has no per-attempt
                // identity, so a late disconnect by id would remove the retry's entry. A registration that outlasts the
                // grace is settled before the connection's disconnect instead, and one that never completes relies on
                // the registry's own lifecycle (an entry flagged as running with a debugger that never pinged is swept as stale too).
                _ = registration.ContinueWith(
                    static task => _ = task.Exception,
                    CancellationToken.None,
                    TaskContinuationOptions.OnlyOnFaulted | TaskContinuationOptions.ExecuteSynchronously,
                    TaskScheduler.Default);
                await registration.WaitAsync(Grace, RegistrationTimeProvider, cancellationToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                // The call was cancelled while registering - not an error.
            }
            catch (TimeoutException)
            {
                logger.ClientRegistrationContinuesInBackground(request.ConnectionId);
                _ = pending.ContinueWith(
                    task => logger.ClientRegistrationFailed(request.ConnectionId, task.Exception!.GetBaseException()),
                    CancellationToken.None,
                    TaskContinuationOptions.OnlyOnFaulted | TaskContinuationOptions.ExecuteSynchronously,
                    TaskScheduler.Default);
            }
            catch (Exception ex)
            {
                logger.ClientRegistrationFailed(request.ConnectionId, ex);
                var status = ex switch
                {
                    RpcException rpcException => new Status(rpcException.StatusCode, "Client registration failed"),
                    _ => new Status(StatusCode.Unknown, "Client registration failed")
                };
                subject.OnError(new RpcException(status));
                return (false, pending);
            }
        }

        if (cancellationToken.IsCancellationRequested)
        {
            subject.OnCompleted();
            return (false, pending);
        }

        return (true, pending);
    }

    ConnectionKeepAlive AliveFor(string connectionId) => new()
    {
        ConnectionId = connectionId,
        Status = (kernelLoad?.Status ?? KernelLoadStatus.Accepting) switch
        {
            KernelLoadStatus.Degraded => KernelStatus.Degraded,
            KernelLoadStatus.Recovering => KernelStatus.Recovering,
            _ => KernelStatus.Accepting
        }
    };

    /// <summary>
    /// Waits for a task for at most the grace, leaving it running when it takes longer.
    /// </summary>
    /// <param name="task">The task to wait for. It handles its own failures.</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/> that ends the wait.</param>
    /// <returns>Awaitable task.</returns>
    async Task WaitForGrace(Task task, CancellationToken cancellationToken)
    {
        try
        {
            await task.WaitAsync(Grace, RegistrationTimeProvider, cancellationToken).ConfigureAwait(false);
        }
        catch (TimeoutException)
        {
            // Slow grains do not hold the next keep-alive back. The task carries on.
        }
    }

    /// <summary>
    /// Waits for a task that may still be running at the end of a connection, for at most the response timeout.
    /// </summary>
    /// <param name="task">The task to wait for.</param>
    /// <returns>Awaitable task.</returns>
    async Task WaitForSettling(Task task)
    {
        try
        {
            await task.WaitAsync(options.Value.Clustering.ResponseTimeout, RegistrationTimeProvider).ConfigureAwait(false);
        }
        catch
        {
            // It reports its own failures; ending the connection must not depend on it.
        }
    }

    async Task HandleResponse(Cratis.Chronicle.Concepts.Clients.ConnectionId connectionId, DateTimeOffset respondedAt)
    {
        try
        {
            var connectedClients = grainFactory.GetConnectedClients(localSiloDetails.SiloAddress);
            var registryKnowsClient = await connectedClients.OnClientPing(connectionId);
            if (!registryKnowsClient && !openConnections.IsOpen(connectionId))
            {
                // The connection was opened on another silo, and the load balancer sent this keep-alive here. Its own
                // silo has to see it, or the client is dropped as stale while it is alive (#5665).
                await PingOwningSilo(connectionId);
                return;
            }

            if (openConnections.Responded(connectionId, registryKnowsClient, respondedAt, _lapse, out var request))
            {
                await Restore(connectedClients, request);
            }
        }
        finally
        {
            openConnections.Handled(connectionId, DateTimeOffset.UtcNow);
        }
    }

    /// <summary>
    /// Refreshes a client running with a debugger in its silo's registry, and registers it again if the registry no
    /// longer knows it while the connection is still open.
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
    /// <para>
    /// A client running with a debugger does not answer keep-alives, so an open connection is the only proof it is
    /// alive. Every keep-alive therefore refreshes its registry entry, which keeps the entry from being swept as
    /// stale. An abandoned registration has no open connection, is never refreshed here and so still expires.
    /// </para>
    /// </remarks>
    /// <param name="connectedClients">The <see cref="IConnectedClients"/> registry of the silo the client is connected to.</param>
    /// <param name="request">The <see cref="ConnectRequest"/> the client connected with.</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/> of the call; once it ends, the client is not put back.</param>
    /// <returns>Awaitable task.</returns>
    async Task KeepRegistered(IConnectedClients connectedClients, ConnectRequest request, CancellationToken cancellationToken)
    {
        try
        {
            if (await connectedClients.OnClientPing(request.ConnectionId) || cancellationToken.IsCancellationRequested)
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
