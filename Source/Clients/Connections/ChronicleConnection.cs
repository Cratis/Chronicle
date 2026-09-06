// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics;
using System.Security.Cryptography.X509Certificates;
using Cratis.Chronicle.Contracts;
using Cratis.Chronicle.Contracts.Captures;
using Cratis.Chronicle.Contracts.Clients;
using Cratis.Chronicle.Contracts.Compliance;
using Cratis.Chronicle.Contracts.Events.Constraints;
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
public sealed class ChronicleConnection : IChronicleConnection, IChronicleServicesAccessor
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
    readonly SemaphoreSlim _connectLock = new(1, 1);
    readonly ConnectionWatchdog _watchDog;
    readonly TimeProvider _timeProvider;
    ChronicleServerAddress? _currentServerAddress;
    GrpcChannel? _channel;
    IConnectionService? _connectionService;
    IServices _services;
    IDisposable? _keepAliveSubscription;
    TaskCompletionSource? _connectTcs;
    DateTimeOffset? _lastConnectFailure;
    bool _hasEverConnected;

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
        _timeProvider = timeProvider ?? TimeProvider.System;
        _skipTlsValidation = skipTlsValidation;
        _skipCompatibilityCheck = skipCompatibilityCheck;
        _skipKeepAlive = skipKeepAlive;
        _connectionString = connectionString;
        _connectTimeout = connectTimeout;
        _maxReceiveMessageSize = maxReceiveMessageSize;
        _maxSendMessageSize = maxSendMessageSize;
        Lifecycle = connectionLifecycle;
        _correlationIdAccessor = correlationIdAccessor;
        _cancellationToken = cancellationToken;
        _logger = logger;
        _loggerFactory = loggerFactory;
        _certificatePath = certificatePath;
        _certificatePassword = certificatePassword;
        _tokenProvider = tokenProvider ?? new NoOpTokenProvider();
        _serverAddressResolver = serverAddressResolver ?? new ChronicleServerAddressResolver();
        _loadBalancerStrategy = loadBalancerStrategy ?? LoadBalancerStrategies.Create(connectionString.LoadBalancer, skipTlsValidation);
        _watchDog = new ConnectionWatchdog(
            tasks,
            OnSessionDropped,
            Reconnect,
            loggerFactory.CreateLogger<ConnectionWatchdog>(),
            cancellationToken);

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
    IServices IChronicleServicesAccessor.Services
    {
        get
        {
            ConnectIfNotConnected();
            return _services;
        }
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        _connectLock.Dispose();
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
    /// <exception cref="ConnectionTimedOut">Thrown when the connect attempt, or the wait for one already in flight, exceeds the connect timeout.</exception>
    /// <exception cref="ConnectionUnavailable">Thrown when a recent attempt failed and the back-off after it has not elapsed yet.</exception>
    /// <remarks>
    /// Every failure mode here has to end in a return or a throw. A client whose reconnect never completes used to
    /// absorb the thread of every caller instead: the connect lock was waited on without a deadline, so callers
    /// queued behind each attempt as well as their own, and an arrival rate above the connect timeout grew that
    /// queue without bound (#3948).
    /// </remarks>
    public Task Connect() => Connect(respectFailureBackoff: true);

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
    internal async Task Reconnect() => await Connect(respectFailureBackoff: false);

    async Task Connect(bool respectFailureBackoff)
    {
        if (Lifecycle.IsConnected)
        {
            return;
        }

        if (respectFailureBackoff)
        {
            ThrowIfWithinConnectFailureBackoff();
        }

        var connectTimeout = TimeSpan.FromSeconds(_connectTimeout);
        if (!await _connectLock.WaitAsync(connectTimeout, _cancellationToken))
        {
            RecordConnectFailure();
            throw new ConnectionTimedOut();
        }

        try
        {
            if (Lifecycle.IsConnected)
            {
                return;
            }

            // The attempt we queued behind may have just failed. Paying for our own full attempt on top of the
            // wait we already served is what turned one unreachable kernel into a thread per caller.
            if (respectFailureBackoff)
            {
                ThrowIfWithinConnectFailureBackoff();
            }

            await ConnectInternal();
            _lastConnectFailure = null;
        }
        catch
        {
            RecordConnectFailure();
            throw;
        }
        finally
        {
            _connectLock.Release();
        }
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
        _logger.Connecting(_connectionString.Redacted);
        _channel?.Dispose();
        _keepAliveSubscription?.Dispose();

        var serverAddresses = await _serverAddressResolver.Resolve(_connectionString);
        _currentServerAddress = await _loadBalancerStrategy.Next(serverAddresses);
        _channel = CreateGrpcChannel(_currentServerAddress);
        var clientFactory = new InProcessAwareGrpcClientProxiesClientFactory();
        var callInvoker = _channel
            .Intercept(new AuthenticationClientInterceptor(_tokenProvider, _loggerFactory.CreateLogger<AuthenticationClientInterceptor>()))
            .Intercept(new CorrelationIdClientInterceptor(_correlationIdAccessor));

        // Perform compatibility check before establishing connection
        if (!_skipCompatibilityCheck)
        {
            await CheckCompatibility(callInvoker.CreateGrpcService<IConnectionService>(clientFactory));
        }

        _services = new Services(
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
            callInvoker.CreateGrpcService<IConnectionService>(clientFactory));

        if (_skipKeepAlive)
        {
            _logger.Connected();
            _hasEverConnected = true;
            await Lifecycle.Connected();
            return;
        }

        _connectionService = callInvoker.CreateGrpcService<IConnectionService>(clientFactory);
        _watchDog.NotifyKeepAlive();
        _connectTcs = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

        _keepAliveSubscription = _connectionService.Connect(
            new()
            {
                ConnectionId = Lifecycle.ConnectionId,
                ClientVersion = ClientProcess.Version,
                IsRunningWithDebugger = Debugger.IsAttached,
                ProcessId = ClientProcess.Id,
                ProcessPath = ClientProcess.Path,
                MachineName = ClientProcess.MachineName,
                ClientType = ChronicleClientIdentity.Type,
            }).Subscribe(HandleConnection);

        try
        {
            await _connectTcs.Task.WaitAsync(TimeSpan.FromSeconds(_connectTimeout), _timeProvider);
            _logger.Connected();
            _hasEverConnected = true;
            await Lifecycle.Connected();
        }
        catch (TimeoutException)
        {
            // Returning normally here reported success while the lifecycle stayed disconnected, so there was no
            // failure state to observe and no back-off to apply - the next call simply re-entered and paid the
            // timeout again (#3948). The watchdog still starts below and keeps reconnecting in the background.
            //
            // Only once a connection has been established, though. A kernel that has not come up yet while its
            // client starts is ordinary, and a host that cannot boot through it is worse than one that keeps
            // trying - so the first connect keeps waiting on the watchdog rather than failing its caller.
            _logger.TimedOut();
            if (_hasEverConnected)
            {
                throw new ConnectionTimedOut();
            }
        }
        finally
        {
            _watchDog.Start();
        }
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
    /// built with and the server does the one comparison. See <c>Source/Kernel/Compatibility</c>.
    /// </remarks>
    async Task CheckCompatibility(IConnectionService connectionService)
    {
        CompatibilityResponse response;

        try
        {
            response = await connectionService.CheckCompatibility(new()
            {
                ClientType = ChronicleClientIdentity.Type,
                ClientVersion = ChronicleClientIdentity.Version,
                ProtocolVersion = ChronicleClientIdentity.ProtocolVersion,
                DescriptorSet = Contracts.WireContractDescriptorSet.Bytes.ToArray()
            });
        }
        catch (RpcException ex) when (ex.StatusCode == StatusCode.Unimplemented)
        {
            // A server from before the check moved server-side does not have this method. Fall back to the older
            // exchange it does have, so upgrading the client does not silently drop the check against those servers.
            await CheckCompatibilityAgainstOlderServer(connectionService);
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

        _logger.CompatibilityCheckPassed(ChronicleClientIdentity.Version, ChronicleClientIdentity.ProtocolVersion, response.ServerVersion, response.ServerProtocolVersion);
    }

    /// <summary>
    /// Runs the pre-server-side compatibility check against a server too old to have <c>CheckCompatibility</c>.
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
                ? CertificateLoader.LoadCertificate(_certificatePath!, _certificatePassword!)
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

            var address = $"https://{serverAddress.Host}:{serverAddress.Port}";

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

    void HandleConnection(ConnectionKeepAlive keepAlive)
    {
        if (_connectTcs?.Task.IsCompleted == false)
        {
            _connectTcs?.SetResult();
        }
        _watchDog.NotifyKeepAlive();

        if (!Debugger.IsAttached)
        {
            _connectionService?.ConnectionKeepAlive(keepAlive).GetAwaiter().GetResult();
        }
    }

    async Task OnSessionDropped()
    {
        if (_connectTcs?.Task.IsCompleted == true)
        {
            _logger.Disconnected();
            await Lifecycle.Disconnected();
        }
    }
}
