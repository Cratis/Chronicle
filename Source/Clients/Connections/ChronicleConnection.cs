// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics;
using System.Security.Cryptography.X509Certificates;
using Cratis.Chronicle.Contracts;
using Cratis.Chronicle.Contracts.Captures;
using Cratis.Chronicle.Contracts.Clients;
using Cratis.Chronicle.Contracts.Compliance;
using Cratis.Chronicle.Contracts.Events.Constraints;
using Cratis.Chronicle.Contracts.EventSources;
using Cratis.Chronicle.Contracts.EventStores;
using Cratis.Chronicle.Contracts.EventTypes;
using Cratis.Chronicle.Contracts.ExternalServices;
using Cratis.Chronicle.Contracts.Host;
using Cratis.Chronicle.Contracts.Identities;
using Cratis.Chronicle.Contracts.Jobs;
using Cratis.Chronicle.Contracts.Namespaces;
using Cratis.Chronicle.Contracts.Observation;
using Cratis.Chronicle.Contracts.Observation.EventStoreSubscriptions;
using Cratis.Chronicle.Contracts.Observation.Reactors;
using Cratis.Chronicle.Contracts.Observation.Reducers;
using Cratis.Chronicle.Contracts.Observation.Webhooks;
using Cratis.Chronicle.Contracts.Projections;
using Cratis.Chronicle.Contracts.Recommendations;
using Cratis.Chronicle.Contracts.Security;
using Cratis.Chronicle.Contracts.Seeding;
using Cratis.Execution;
using Cratis.Tasks;
using Grpc.Core;
using Grpc.Core.Interceptors;
using Grpc.Net.Client;
using Grpc.Net.Client.Configuration;
using Microsoft.Extensions.Logging;
using ProtoBuf.Grpc.Client;

namespace Cratis.Chronicle.Connections;

/// <summary>
/// Represents an implementation of <see cref="IChronicleConnection"/>.
/// </summary>
public sealed class ChronicleConnection : IChronicleConnection, IChronicleServicesAccessor, IGetServicesAsynchronously, IKernelCapabilities, IConnectionKeepAlive
{
    readonly ChronicleConnectionString _connectionString;
    readonly int _connectTimeout;
    readonly int? _maxReceiveMessageSize;
    readonly int? _maxSendMessageSize;
    readonly ICorrelationIdAccessor _correlationIdAccessor;
    readonly CancellationToken _cancellationToken;
    readonly ILogger<ChronicleConnection> _logger;
    readonly ILoggerFactory _loggerFactory;
    readonly string? _certificatePath;
    readonly string? _certificatePassword;
    readonly ITokenProvider _tokenProvider;
    readonly bool _skipTlsValidation;
    readonly bool _skipCompatibilityCheck;
    readonly bool _skipKeepAlive;
    readonly IChronicleServerAddressResolver _serverAddressResolver;
    readonly ILoadBalancerStrategy _loadBalancerStrategy;
    readonly object _attemptGate = new();
    readonly ConnectionWatchdog _watchDog;
    readonly TimeProvider _timeProvider;
    ChronicleServerAddress? _currentServerAddress;
    GrpcChannel? _channel;
    IConnectionService? _connectionService;
    IServices _services;
    IDisposable? _keepAliveSubscription;
    TaskCompletionSource? _connectTcs;
    ChronicleServerAddress? _lastDroppedAddress;
    DateTimeOffset? _lastConnectFailure;
    bool _hasEverConnected;
    ConnectAttempt? _inFlight;

    /// <summary>
    /// Initializes a new instance of the <see cref="ChronicleConnection"/> class.
    /// </summary>
    /// <param name="connectionString"><see cref="ChronicleConnectionString"/> to connect with.</param>
    /// <param name="connectTimeout">Timeout when connecting in seconds.</param>
    /// <param name="maxReceiveMessageSize">Maximum receive message size in bytes.</param>
    /// <param name="maxSendMessageSize">Maximum send message size in bytes.</param>
    /// <param name="connectionLifecycle"><see cref="IConnectionLifecycle"/> for when connection state changes.</param>
    /// <param name="tasks"><see cref="ITaskFactory"/> to create tasks with.</param>
    /// <param name="correlationIdAccessor"><see cref="ICorrelationIdAccessor"/> to access the correlation ID.</param>
    /// <param name="loggerFactory">Logger factory for creating loggers.</param>
    /// <param name="cancellationToken">The clients <see cref="CancellationToken"/>.</param>
    /// <param name="logger"><see cref="ILogger{TCategoryName}"/> for diagnostics.</param>
    /// <param name="skipTlsValidation">Whether to skip TLS certificate validation for the connection.</param>
    /// <param name="certificatePath">Optional path to the certificate file.</param>
    /// <param name="certificatePassword">Optional password for the certificate file.</param>
    /// <param name="tokenProvider"><see cref="ITokenProvider"/> for authentication.</param>
    /// <param name="skipCompatibilityCheck">Whether to skip the server compatibility check on connect. Useful for short-lived clients like CLIs.</param>
    /// <param name="skipKeepAlive">Whether to skip the keep-alive handshake on connect. Useful for short-lived clients like CLIs.</param>
    /// <param name="serverAddressResolver">Optional <see cref="IChronicleServerAddressResolver"/> for resolving server addresses. Defaults to <see cref="ChronicleServerAddressResolver"/>.</param>
    /// <param name="loadBalancerStrategy">Optional <see cref="ILoadBalancerStrategy"/> for selecting among multiple servers. Defaults to the strategy named by the connection string, or least-connections.</param>
    /// <param name="timeProvider">Optional <see cref="TimeProvider"/> used to time the back-off after a failed connect. Defaults to the system clock.</param>
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
#pragma warning disable CA1068 // CancellationToken parameters must come last
    public ChronicleConnection(
        ChronicleConnectionString connectionString,
        int connectTimeout,
        int? maxReceiveMessageSize,
        int? maxSendMessageSize,
        IConnectionLifecycle connectionLifecycle,
        ITaskFactory tasks,
        ICorrelationIdAccessor correlationIdAccessor,
        ILoggerFactory loggerFactory,
        CancellationToken cancellationToken,
        ILogger<ChronicleConnection> logger,
        bool skipTlsValidation,
        string? certificatePath = null,
        string? certificatePassword = null,
        ITokenProvider? tokenProvider = null,
        bool skipCompatibilityCheck = false,
        bool skipKeepAlive = false,
        IChronicleServerAddressResolver? serverAddressResolver = null,
        ILoadBalancerStrategy? loadBalancerStrategy = null,
        TimeProvider? timeProvider = null)
    {
        if (connectionString.DisableTls && connectionString.AuthenticationMode == AuthenticationMode.ClientCredentials)
        {
            throw new ClientCredentialsRequireTls();
        }

        _timeProvider = timeProvider ?? TimeProvider.System;
        _skipTlsValidation = skipTlsValidation;
        _skipCompatibilityCheck = skipCompatibilityCheck;
        _skipKeepAlive = skipKeepAlive || connectionString.SkipKeepAlive;
        _connectionString = connectionString;
        _connectTimeout = connectTimeout;
        _maxReceiveMessageSize = maxReceiveMessageSize;
        _maxSendMessageSize = maxSendMessageSize;
        Lifecycle = connectionLifecycle;
        _correlationIdAccessor = correlationIdAccessor;
        _cancellationToken = cancellationToken;
        Lifecycle.BoundDisconnectFence(TimeSpan.FromSeconds(connectTimeout), _timeProvider, cancellationToken);
        _logger = logger;
        _loggerFactory = loggerFactory;
        if (ServerCertificateValidationWarning.AppliesTo(skipTlsValidation, connectionString))
        {
            _logger.ServerCertificateValidationDisabled(string.Join(", ", connectionString.ServerAddresses));
        }

        _certificatePath = certificatePath;
        _certificatePassword = certificatePassword;
        _tokenProvider = tokenProvider ?? new NoOpTokenProvider();
        _serverAddressResolver = serverAddressResolver ?? new ChronicleServerAddressResolver();
        _loadBalancerStrategy = loadBalancerStrategy ?? LoadBalancerStrategies.Create(connectionString.LoadBalancer, skipTlsValidation, connectionString.DisableTls);
        _watchDog = new ConnectionWatchdog(
            tasks,
            OnSessionDropped,
            Reconnect,
            loggerFactory.CreateLogger<ConnectionWatchdog>(),
            cancellationToken,
            _timeProvider);

        _cancellationToken.Register(() =>
        {
            _connectTcs?.TrySetCanceled();
            _keepAliveSubscription?.Dispose();
            _channel?.ShutdownAsync().GetAwaiter().GetResult();
            _channel?.Dispose();
        });
    }
#pragma warning restore CA1068 // CancellationToken parameters must come last
#pragma warning restore CS8618

    /// <inheritdoc/>
    public IConnectionLifecycle Lifecycle { get; }

    /// <summary>
    /// Gets the <see cref="ChronicleServerAddress"/> the connection is currently using, or the
    /// first configured address when no connection has been established yet.
    /// </summary>
    public ChronicleServerAddress CurrentServerAddress => _currentServerAddress ?? _connectionString.ServerAddress;

    /// <inheritdoc/>
    public IReadOnlyCollection<string> Capabilities { get; private set; } = [];

    /// <inheritdoc/>
    public bool CapabilitiesAreKnown { get; private set; }

    /// <inheritdoc/>
    bool IConnectionKeepAlive.IsSkipped => _skipKeepAlive;

    /// <inheritdoc/>
    IServices IChronicleServicesAccessor.Services
    {
        get
        {
            ConnectIfNotConnected();
            return _services;
        }
    }

    /// <summary>
    /// Gets the longest a connect attempt waits for one server to complete its handshake while another server is
    /// still untried. The last untried server gets whatever remains of the connect timeout.
    /// </summary>
    internal TimeSpan EndpointHandshakeTimeout { get; init; } = TimeSpan.FromSeconds(5);

    /// <summary>
    /// Gets how the connection service for a server is created. Specs replace this to fake the keep-alive stream per server.
    /// </summary>
    internal Func<ChronicleServerAddress, CallInvoker, IConnectionService>? ConnectionServiceFactory { get; init; }

    /// <inheritdoc/>
    public async ValueTask<IServices> GetServices(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (!Lifecycle.IsConnected)
        {
            await Connect(respectFailureBackoff: true, cancellationToken);
        }

        return _services;
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        _channel?.Dispose();
        _keepAliveSubscription?.Dispose();
        if (_tokenProvider is IDisposable disposableTokenProvider)
        {
            disposableTokenProvider.Dispose();
        }
        if (_loadBalancerStrategy is IDisposable disposableLoadBalancerStrategy)
        {
            disposableLoadBalancerStrategy.Dispose();
        }
    }

    /// <inheritdoc/>
    /// <exception cref="ConnectionTimedOut">Thrown when the connect attempt this caller started exceeds the connect timeout, or when this caller's wait for an attempt already in flight exceeds the connect timeout in either the transport or the connected-handler phase; the attempt itself continues.</exception>
    /// <exception cref="ConnectionUnavailable">Thrown when a recent attempt failed and the back-off after it has not elapsed yet.</exception>
    /// <remarks>
    /// Every failure mode here has to end in a return or a throw. A client whose reconnect never completes used to
    /// absorb the thread of every caller instead: the connect lock was waited on without a deadline, so callers
    /// queued behind each attempt as well as their own, and an arrival rate above the connect timeout grew that
    /// queue without bound (#3948). Callers now join the one attempt in flight, each with a deadline of its own.
    /// </remarks>
    public Task Connect() => Connect(respectFailureBackoff: true, CancellationToken.None);

    /// <inheritdoc/>
    public void RequestReconnect() => _watchDog.RequestReconnect();

    /// <summary>
    /// Reconnect on behalf of the watchdog, which must always get to attempt.
    /// </summary>
    /// <returns>Awaitable task.</returns>
    /// <remarks>
    /// The watchdog is the thing that recovers the connection, and it paces itself with its own backoff. Holding it
    /// to the caller-facing one meant it was refused before it dialed anything, so it burned its attempts on a
    /// connection that could then never come back - every later call kept failing against a kernel that was
    /// reachable again.
    /// </remarks>
    internal async Task Reconnect() => await Connect(respectFailureBackoff: false, CancellationToken.None);

    /// <summary>
    /// Handles a timed-out connection attempt, failing the caller only after a connection has been established.
    /// </summary>
    /// <exception cref="ConnectionTimedOut">Thrown when an established connection times out.</exception>
    internal void HandleConnectTimedOut()
    {
        if (_hasEverConnected)
        {
            _logger.TimedOut();
            throw new ConnectionTimedOut();
        }

        _logger.NotAcceptingConnectionsYet(CurrentServerAddress.ToString(), _connectTimeout);
    }

    /// <summary>
    /// Asks the server whether it still serves what this client expects, and refuses to connect when it does not.
    /// </summary>
    /// <param name="connectionService">The connection service to ask.</param>
    /// <returns>Awaitable task.</returns>
    /// <exception cref="IncompatibleServerException">Thrown when the server no longer serves this client's contracts.</exception>
    /// <remarks>
    /// The comparison itself is the server's, not this client's - Chronicle has clients in four languages and only
    /// two of those can build a descriptor set at runtime, so every client ships the one its contracts package was
    /// built with and the server does the one comparison. See <c language="csharp">Source/Kernel/Compatibility</c>.
    /// </remarks>
    internal async Task CheckCompatibility(IConnectionService connectionService)
    {
        Capabilities = [];
        CapabilitiesAreKnown = false;
        CompatibilityResponse response;

        try
        {
            response = await connectionService.CheckCompatibility(new()
            {
                ClientType = ChronicleClientIdentity.Type,
                ClientVersion = ChronicleClientIdentity.Version,
                ProtocolVersion = ChronicleClientIdentity.ProtocolVersion,
                DescriptorSet = WireContractDescriptorSet.Bytes.ToArray()
            });
        }
        catch (RpcException ex) when (ex.StatusCode == StatusCode.Unimplemented)
        {
            // A server from before the check moved server-side does not have this method. Fall back to the older
            // exchange it does have, so upgrading the client does not silently drop the check against those servers.
            // Such a server predates the capabilities handshake, so it is known to advertise no capabilities.
            CapabilitiesAreKnown = true;
            await CheckCompatibilityAgainstOlderServer(connectionService);
            return;
        }
        catch (RpcException ex) when (ex.StatusCode == StatusCode.Unavailable)
        {
            // The connect attempt's own outcome (warning, error or connected) reports the transport state.
            _logger.CompatibilityCheckUnreachable(ex.Message);
            return;
        }
        catch (RpcException ex)
        {
            // Not being able to ask says nothing about whether the two sides agree, and a transport problem here
            // would surface again on the very next call with a better message than this one could give.
            _logger.FailedToCheckCompatibility(ex.Message);
            return;
        }

        if (!response.IsCompatible)
        {
            var message = IncompatibilityMessage.Build(
                _currentServerAddress?.ToString() ?? _connectionString.Redacted,
                response.ServerVersion,
                response.ServerProtocolVersion,
                response.Incompatibilities);

            _logger.IncompatibleWithServer(message);
            throw new IncompatibleServerException(message);
        }

        Capabilities = response.Capabilities.ToArray();
        CapabilitiesAreKnown = true;
        _logger.CompatibilityCheckPassed(ChronicleClientIdentity.Version, ChronicleClientIdentity.ProtocolVersion, response.ServerVersion, response.ServerProtocolVersion);
    }

    async Task Connect(bool respectFailureBackoff, CancellationToken waiterToken)
    {
        // Once the attempt in flight has passed the transport phase it may be about to be overtaken by a disconnect, so
        // a late caller waits for it to finish and starts a fresh one. It does that once: after that it joins whatever is
        // in flight, so continuous churn from other callers cannot keep it looping.
        var mayJoinEstablished = false;
        while (true)
        {
            if (Lifecycle.IsConnected)
            {
                return;
            }

            if (respectFailureBackoff)
            {
                ThrowIfWithinConnectFailureBackoff();
            }

            ConnectAttempt attempt;
            bool isStarter;
            lock (_attemptGate)
            {
                isStarter = _inFlight is null;
                if (isStarter)
                {
                    _inFlight = new ConnectAttempt();
                }

                attempt = _inFlight!;
            }

            if (isStarter)
            {
                // Detached, so no waiter's ambient state or token flows into an attempt that every other waiter shares.
                _ = ExecutionContextDetached.Start(() => Task.Run(() => RunAttempt(respectFailureBackoff, attempt)));
                await attempt.Completion.Task.WaitAsync(waiterToken);
                return;
            }

            if (mayJoinEstablished || !attempt.TransportEstablished.Task.IsCompleted)
            {
                await JoinAttempt(attempt, waiterToken);
                return;
            }

            await WaitForAttemptToFinish(attempt, waiterToken);
            mayJoinEstablished = true;
        }
    }

    async Task JoinAttempt(ConnectAttempt attempt, CancellationToken waiterToken)
    {
        var deadline = TimeSpan.FromSeconds(_connectTimeout);
        using var linked = CancellationTokenSource.CreateLinkedTokenSource(waiterToken, _cancellationToken);
        try
        {
            // A fresh deadline per phase: the time the connected handlers take is not charged to the transport.
            await attempt.TransportEstablished.Task.WaitAsync(deadline, _timeProvider, linked.Token);
            await attempt.Completion.Task.WaitAsync(deadline, _timeProvider, linked.Token);
        }
        catch (TimeoutException)
        {
            // This waiter's patience ran out, not the kernel's: the attempt continues and no back-off is armed.
            throw new ConnectionTimedOut();
        }
    }

    async Task WaitForAttemptToFinish(ConnectAttempt attempt, CancellationToken waiterToken)
    {
        using var linked = CancellationTokenSource.CreateLinkedTokenSource(waiterToken, _cancellationToken);
        try
        {
            await attempt.Completion.Task.WaitAsync(TimeSpan.FromSeconds(_connectTimeout), _timeProvider, linked.Token);
        }
        catch (TimeoutException)
        {
            throw new ConnectionTimedOut();
        }
        catch (Exception) when (attempt.Completion.Task.IsFaulted && !linked.IsCancellationRequested)
        {
            // The attempt's own failure belongs to its waiters; this caller decides again from the connection's state.
        }
    }

    async Task RunAttempt(bool respectFailureBackoff, ConnectAttempt attempt)
    {
        Exception? failure = null;
        try
        {
            if (!Lifecycle.IsConnected)
            {
                // The attempt we queued behind may have just failed. Paying for our own full attempt on top of the
                // wait we already served is what turned one unreachable kernel into a thread per caller.
                if (respectFailureBackoff)
                {
                    ThrowIfWithinConnectFailureBackoff();
                }

                await ConnectInternal();
                _lastConnectFailure = null;
            }
        }
        catch (Exception ex)
        {
            RecordConnectFailure();
            failure = ex;
        }

        attempt.TransportEstablished.TrySetResult();
        lock (_attemptGate)
        {
            if (ReferenceEquals(_inFlight, attempt))
            {
                _inFlight = null;
            }
        }

        // Last, and only after the attempt is no longer in flight, so a waiter that wakes up and asks again starts a new one.
        if (failure is null)
        {
            attempt.Completion.TrySetResult();
        }
        else
        {
            attempt.Completion.TrySetException(failure);
        }
    }

    void SignalTransportEstablished()
    {
        ConnectAttempt? attempt;
        lock (_attemptGate)
        {
            attempt = _inFlight;
        }

        attempt?.TransportEstablished.TrySetResult();
    }

    /// <summary>
    /// Records that a connect attempt failed, starting the back-off window that makes subsequent callers fail fast.
    /// </summary>
    /// <remarks>
    /// Only once a connection has actually been established. A kernel that is not up yet while its client starts is
    /// ordinary - the client is expected to keep trying and come good - so the failure state deliberately does not
    /// arm before the first successful connect. What #3948 reports is the other case: a connection that worked and
    /// then stopped recovering, where every later call paid for an attempt of its own.
    /// </remarks>
    void RecordConnectFailure()
    {
        if (_hasEverConnected)
        {
            _lastConnectFailure = _timeProvider.GetUtcNow();
        }
    }

    /// <summary>
    /// Fail immediately while the back-off after a failed connect attempt is still running.
    /// </summary>
    /// <exception cref="ConnectionUnavailable">Thrown when the back-off has not elapsed.</exception>
    /// <remarks>
    /// Without this every caller pays the full connect timeout on its own thread for as long as the kernel stays
    /// unreachable. The window is the connect timeout, so at most one attempt per timeout probes the kernel while
    /// the watchdog keeps reconnecting in the background - and a caller learns the connection is down at once
    /// rather than by waiting for it.
    /// </remarks>
    void ThrowIfWithinConnectFailureBackoff()
    {
        var lastFailure = _lastConnectFailure;
        if (lastFailure is not null && _timeProvider.GetUtcNow() - lastFailure.Value < TimeSpan.FromSeconds(_connectTimeout))
        {
            throw new ConnectionUnavailable(_connectionString.Redacted);
        }
    }

    async Task ConnectInternal()
    {
        // The handshake carries the connection id, and a disconnect still running rotates it when it completes. Reading it
        // before then would give the kernel an id the connected handlers never use.
        await Lifecycle.DisconnectCompleted();

        _logger.Connecting(_connectionString.Redacted);
        DisposeCurrentAttempt();

        var untried = (await _serverAddressResolver.Resolve(_connectionString)).ToList();
        var connected = await TryConnectToAnyCandidate(untried);

        if (_skipKeepAlive)
        {
            // Nothing keeps retrying without the watchdog, so a connect that reached no endpoint has to fail its
            // caller; returning normally would report success with no services and no connected lifecycle (#3948).
            if (!connected)
            {
                throw new ConnectionTimedOut();
            }

            return;
        }

        // Re-arm the staleness clock right before the watchdog starts evaluating it. The clock was last set
        // when the keep-alive stream was subscribed - potentially a full connect timeout ago if the wait timed
        // out. Starting the watchdog on that stale a timestamp let it declare the session dropped on its very
        // first tick, against a connection that never finished establishing (or, on a kernel slow to answer
        // during its own restart, one still legitimately coming up) - tearing the channel down and immediately
        // reconnecting on top of an attempt that might still have succeeded.
        _watchDog.NotifyKeepAlive();
        await ExecutionContextDetached.Start(() => Task.Run(_watchDog.Start));

        if (!connected)
        {
            // Returning normally here reported success while the lifecycle stayed disconnected, so there was no
            // failure state to observe and no back-off to apply - the next call simply re-entered and paid the
            // timeout again (#3948). The watchdog keeps reconnecting in the background.
            //
            // Only once a connection has been established, though. A kernel that has not come up yet while its
            // client starts is ordinary, and a host that cannot boot through it is worse than one that keeps
            // trying - so the first connect keeps waiting on the watchdog rather than failing its caller.
            HandleConnectTimedOut();
        }
    }

    /// <summary>
    /// Tries the untried servers in turn until one completes its handshake or the connect budget is spent.
    /// </summary>
    /// <param name="untried">The servers not yet tried. Picked ones are removed.</param>
    /// <returns>True if a server completed its handshake.</returns>
    async Task<bool> TryConnectToAnyCandidate(List<ChronicleServerAddress> untried)
    {
        var budget = TimeSpan.FromSeconds(_connectTimeout);
        var started = _timeProvider.GetUtcNow();

        // A single endpoint that never answers must not take the whole budget while another one is up, so each
        // endpoint but the last gets a bounded handshake. The last one gets whatever remains, which for a single
        // address is the full connect timeout - exactly what it always was.
        while (true)
        {
            var address = await TakeNextCandidate(untried);
            var remaining = budget - (_timeProvider.GetUtcNow() - started);
            var attemptBudget = untried.Count == 0 ? remaining : TimeSpan.FromTicks(Math.Min(EndpointHandshakeTimeout.Ticks, remaining.Ticks));
            if (await TryConnectTo(address, attemptBudget))
            {
                return true;
            }

            if (untried.Count == 0 || _timeProvider.GetUtcNow() - started >= budget)
            {
                return false;
            }

            DisposeCurrentAttempt();
        }
    }

    void DisposeCurrentAttempt()
    {
        _channel?.Dispose();
        _keepAliveSubscription?.Dispose();
    }

    /// <summary>
    /// Picks the next server to try, asking the load balancer among those not yet tried in this connect.
    /// </summary>
    /// <param name="untried">The servers not yet tried. The picked one is removed.</param>
    /// <returns>The server to try.</returns>
    /// <remarks>
    /// The server whose session just ended is left for last while others remain - it is the one most likely to be
    /// restarting, and a node that has just come back reports no connections, which the least-connections
    /// strategy would otherwise prefer.
    /// </remarks>
    async Task<ChronicleServerAddress> TakeNextCandidate(List<ChronicleServerAddress> untried)
    {
        var pool = untried;
        if (untried.Count > 1 && _lastDroppedAddress is not null)
        {
            var others = untried.Where(address => address != _lastDroppedAddress).ToList();
            if (others.Count > 0)
            {
                pool = others;
            }
        }

        var next = await _loadBalancerStrategy.Next(pool);
        if (!untried.Remove(next))
        {
            untried.Clear();
        }

        return next;
    }

    /// <summary>
    /// Tries to establish the connection against one server.
    /// </summary>
    /// <param name="address">The server to connect to.</param>
    /// <param name="budget">How long to wait for the server to complete its handshake.</param>
    /// <returns>True if the connection was established, false if the handshake failed or ran out of time.</returns>
    async Task<bool> TryConnectTo(ChronicleServerAddress address, TimeSpan budget)
    {
        var attemptStarted = _timeProvider.GetTimestamp();
        _currentServerAddress = address;
        _channel = CreateGrpcChannel(address);
        var clientFactory = new InProcessAwareGrpcClientProxiesClientFactory();
        var callInvoker = _channel
            .Intercept(new AuthenticationClientInterceptor(_tokenProvider, _loggerFactory.CreateLogger<AuthenticationClientInterceptor>()))
            .Intercept(new CorrelationIdClientInterceptor(_correlationIdAccessor));

        // Perform compatibility check before establishing connection
        if (!_skipCompatibilityCheck)
        {
            try
            {
                var compatibilityBudget = budget - _timeProvider.GetElapsedTime(attemptStarted);
                await CheckCompatibility(CreateConnectionService(address, callInvoker, clientFactory))
                    .WaitAsync(compatibilityBudget > TimeSpan.Zero ? compatibilityBudget : TimeSpan.Zero, _timeProvider);
            }
            catch (TimeoutException)
            {
                _logger.EndpointHandshakeFailed(address.ToString(), "the compatibility check timed out");
                return false;
            }
        }

        _services = CreateServices(callInvoker, clientFactory);

        if (_skipKeepAlive)
        {
            _logger.Connected();
            _hasEverConnected = true;
            SignalTransportEstablished();
            await ExecutionContextDetached.Start(() => Task.Run(Lifecycle.Connected));
            return true;
        }

        // The compatibility check and the handshake share one budget, so a slow check cannot double the time spent on an endpoint.
        var remaining = budget - _timeProvider.GetElapsedTime(attemptStarted);
        return await AwaitHandshake(address, callInvoker, clientFactory, remaining > TimeSpan.Zero ? remaining : TimeSpan.Zero);
    }

    IConnectionService CreateConnectionService(ChronicleServerAddress address, CallInvoker callInvoker, InProcessAwareGrpcClientProxiesClientFactory clientFactory) =>
        ConnectionServiceFactory?.Invoke(address, callInvoker) ?? callInvoker.CreateGrpcService<IConnectionService>(clientFactory);

    Services CreateServices(CallInvoker callInvoker, InProcessAwareGrpcClientProxiesClientFactory clientFactory)
    {
        return new Services(
            callInvoker.CreateGrpcService<ICompliance>(clientFactory),
            callInvoker.CreateGrpcService<IEventStores>(clientFactory),
            callInvoker.CreateGrpcService<INamespaces>(clientFactory),
            callInvoker.CreateGrpcService<IRecommendations>(clientFactory),
            callInvoker.CreateGrpcService<Contracts.Patterns.IPatterns>(clientFactory),
            callInvoker.CreateGrpcService<IIdentities>(clientFactory),
            callInvoker.CreateGrpcService<Contracts.Sequences.IEventSequences>(clientFactory),
            callInvoker.CreateGrpcService<IEventTypes>(clientFactory),
            callInvoker.CreateGrpcService<IConstraints>(clientFactory),
            callInvoker.CreateGrpcService<IObservers>(clientFactory),
            callInvoker.CreateGrpcService<IFailedPartitions>(clientFactory),
            callInvoker.CreateGrpcService<IReactors>(clientFactory),
            callInvoker.CreateGrpcService<IReducers>(clientFactory),
            callInvoker.CreateGrpcService<IProjections>(clientFactory),
            callInvoker.CreateGrpcService<IWebhooks>(clientFactory),
            callInvoker.CreateGrpcService<IExternalServices>(clientFactory),
            callInvoker.CreateGrpcService<ICaptures>(clientFactory),
            callInvoker.CreateGrpcService<IEventStoreSubscriptions>(clientFactory),
            callInvoker.CreateGrpcService<Contracts.ReadModels.IReadModels>(clientFactory),
            callInvoker.CreateGrpcService<Contracts.ReadModels.IMaterializedReadModels>(clientFactory),
            callInvoker.CreateGrpcService<Contracts.ReadModelExplorer.IReadModelExplorer>(clientFactory),
            callInvoker.CreateGrpcService<IJobs>(clientFactory),
            callInvoker.CreateGrpcService<IEventSeeding>(clientFactory),
            callInvoker.CreateGrpcService<IUsers>(clientFactory),
            callInvoker.CreateGrpcService<IApplications>(clientFactory),
            callInvoker.CreateGrpcService<IServer>(clientFactory),
            callInvoker.CreateGrpcService<IConnectionService>(clientFactory),
            callInvoker.CreateGrpcService<IEventSources>(clientFactory))
        {
            Administrators = callInvoker.CreateGrpcService<Contracts.Security.IAdministrators>(clientFactory),
            Occurrences = callInvoker.CreateGrpcService<Contracts.Occurrences.IOccurrences>(clientFactory)
        };
    }

    async Task<bool> AwaitHandshake(ChronicleServerAddress address, CallInvoker callInvoker, InProcessAwareGrpcClientProxiesClientFactory clientFactory, TimeSpan budget)
    {
        _connectionService = CreateConnectionService(address, callInvoker, clientFactory);
        _watchDog.NotifyKeepAlive();
        var handshake = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        _connectTcs = handshake;

        await ExecutionContextDetached.Start(() => Task.Run(() => _keepAliveSubscription = _connectionService.Connect(
            new()
            {
                ConnectionId = Lifecycle.ConnectionId,
                ClientVersion = ClientProcess.Version,
                IsRunningWithDebugger = Debugger.IsAttached,
                ProcessId = ClientProcess.Id,
                ProcessPath = ClientProcess.Path,
                MachineName = ClientProcess.MachineName,
                ClientType = ChronicleClientIdentity.Type,
            }).Subscribe(
                keepAlive => HandleConnection(handshake, keepAlive),
                error => HandleStreamEnded(handshake, address, error),
                () => HandleStreamEnded(handshake, address, null))));

        try
        {
            await handshake.Task.WaitAsync(budget, _timeProvider);
            _logger.Connected();
            _hasEverConnected = true;
            SignalTransportEstablished();
            await ExecutionContextDetached.Start(() => Task.Run(Lifecycle.Connected));
            return true;
        }
        catch (Exception ex) when (ex is TimeoutException or ConnectionUnavailable or RpcException)
        {
            _logger.EndpointHandshakeFailed(address.ToString(), ex.Message);
            return false;
        }
    }

    /// <summary>
    /// Runs the pre-server-side compatibility check against a server too old to have <c language="csharp">CheckCompatibility</c>.
    /// </summary>
    /// <param name="connectionService">The connection service to ask.</param>
    /// <returns>Awaitable task.</returns>
    /// <exception cref="IncompatibleServerException">Thrown when the server no longer serves this client's contracts.</exception>
    async Task CheckCompatibilityAgainstOlderServer(IConnectionService connectionService)
    {
        try
        {
            var serverSchema = await connectionService.GetDescriptorSet();
            var result = CompatibilityValidator.Validate(
                CompatibilityValidator.GenerateClientSchema(),
                serverSchema.SchemaDefinition,
                _logger);

            if (!result.IsCompatible)
            {
                var message = IncompatibilityMessage.Build(
                    _currentServerAddress?.ToString() ?? _connectionString.Redacted,
                    serverVersion: string.Empty,
                    serverProtocolVersion: string.Empty,
                    result.Errors);

                _logger.IncompatibleWithServer(message);
                throw new IncompatibleServerException(message);
            }

            _logger.CompatibilityCheckPassed(ChronicleClientIdentity.Version, ChronicleClientIdentity.ProtocolVersion, "unknown", "unknown");
        }
        catch (RpcException ex)
        {
            _logger.FailedToRetrieveServerDescriptorSet(ex.Message);
        }
    }

    void ConnectIfNotConnected()
    {
        if (!Lifecycle.IsConnected)
        {
            Connect().GetAwaiter().GetResult();
        }
    }

    GrpcChannel CreateGrpcChannel(ChronicleServerAddress serverAddress)
    {
        X509Certificate2? certificate = null;
        try
        {
#pragma warning disable CA2000 // Certificate ownership is transferred to httpHandler.SslOptions.ClientCertificates
            // Only load a client certificate when one is configured. Without a certificate the client
            // still connects over TLS (server-authenticated) — it simply does not present a client
            // certificate for mutual TLS. This is the common case against a TLS server with no mutual-TLS.
            certificate = !string.IsNullOrEmpty(_certificatePath)
                ? CertificateLoader.LoadCertificate(_certificatePath!, _certificatePassword)
                : null;
            var httpHandler = new SocketsHttpHandler
            {
                PooledConnectionIdleTimeout = Timeout.InfiniteTimeSpan,
                KeepAlivePingDelay = TimeSpan.FromSeconds(60),
                KeepAlivePingTimeout = TimeSpan.FromSeconds(30),
                EnableMultipleHttp2Connections = true
            };

            if (certificate is not null)
            {
                httpHandler.SslOptions.ClientCertificates = new X509CertificateCollection { certificate };
                _logger.UsingClientCertificate(_certificatePath!);
            }
#pragma warning restore CA2000

            httpHandler.SslOptions.RemoteCertificateValidationCallback =
                CertificateLoader.CreateServerCertificateValidationCallback(_skipTlsValidation, certificate?.GetCertHashString());

            // With TLS disabled the server speaks HTTP/2 without TLS (h2c); an http:// address makes the gRPC channel
            // use HTTP/2 with prior knowledge.
            var address = $"{(_connectionString.DisableTls ? Uri.UriSchemeHttp : Uri.UriSchemeHttps)}://{serverAddress.Host}:{serverAddress.Port}";

            var channel = GrpcChannel.ForAddress(
                address,
                new GrpcChannelOptions
                {
                    HttpHandler = httpHandler,
                    MaxReceiveMessageSize = _maxReceiveMessageSize,
                    MaxSendMessageSize = _maxSendMessageSize,
                    ServiceConfig = new ServiceConfig
                    {
                        MethodConfigs =
                        {
                            new MethodConfig
                            {
                                Names = { MethodName.Default },
                                RetryPolicy = new RetryPolicy
                                {
                                    MaxAttempts = 5,
                                    InitialBackoff = TimeSpan.FromSeconds(1),
                                    MaxBackoff = TimeSpan.FromSeconds(10),
                                    BackoffMultiplier = 1.5,
                                    RetryableStatusCodes = { StatusCode.Unavailable }
                                }
                            }
                        }
                    }
                });

            _logger.ChannelCreated(address);
            return channel;
        }
        catch
        {
            certificate?.Dispose();
            throw;
        }
    }

    void HandleConnection(TaskCompletionSource handshake, ConnectionKeepAlive keepAlive)
    {
        // A late callback from an attempt that has since been replaced must not touch the current one.
        if (!ReferenceEquals(_connectTcs, handshake))
        {
            return;
        }

        handshake.TrySetResult();
        _watchDog.NotifyKeepAlive();

        if (!Debugger.IsAttached)
        {
            _connectionService?.ConnectionKeepAlive(keepAlive).GetAwaiter().GetResult();
        }
    }

    /// <summary>
    /// Reacts to the keep-alive stream ending, whether it failed or completed.
    /// </summary>
    /// <param name="handshake">The attempt the stream belongs to.</param>
    /// <param name="address">The server the stream was opened against.</param>
    /// <param name="error">The failure, or null when the stream completed.</param>
    /// <remarks>
    /// Before the first keep-alive this fails the handshake at once so the next server is tried without waiting
    /// out the timeout. After it, the session is gone, so the watchdog is asked to reconnect now rather than
    /// after the keep-alive has gone stale.
    /// </remarks>
    void HandleStreamEnded(TaskCompletionSource handshake, ChronicleServerAddress address, Exception? error)
    {
        if (!ReferenceEquals(_connectTcs, handshake))
        {
            return;
        }

        if (handshake.TrySetException(error ?? new ConnectionUnavailable(_connectionString.Redacted)))
        {
            return;
        }

        if (handshake.Task.IsCompletedSuccessfully)
        {
            _lastDroppedAddress = address;
            _logger.KeepAliveStreamEnded(address.ToString());
            _watchDog.RequestReconnect();
        }
    }

    async Task OnSessionDropped()
    {
        if (_connectTcs?.Task.IsCompletedSuccessfully == true)
        {
            // The session went silent rather than ending, so the stream never reports it: remember the node here
            // so the next connect tries the others first.
            _lastDroppedAddress = _currentServerAddress;
            _logger.Disconnected();
            await Lifecycle.Disconnected();
        }
    }
}
