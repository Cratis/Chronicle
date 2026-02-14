// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
using Cratis.Chronicle.Contracts;
using Cratis.Chronicle.Contracts.Clients;
using Cratis.Chronicle.Contracts.Events;
using Cratis.Chronicle.Contracts.Events.Constraints;
using Cratis.Chronicle.Contracts.EventSequences;
using Cratis.Chronicle.Contracts.Host;
using Cratis.Chronicle.Contracts.Identities;
using Cratis.Chronicle.Contracts.Jobs;
using Cratis.Chronicle.Contracts.Observation;
using Cratis.Chronicle.Contracts.Observation.Reactors;
using Cratis.Chronicle.Contracts.Observation.Reducers;
using Cratis.Chronicle.Contracts.Observation.Webhooks;
using Cratis.Chronicle.Contracts.Projections;
using Cratis.Chronicle.Contracts.ReadModels;
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
    readonly ITaskFactory _tasks;
    readonly ICorrelationIdAccessor _correlationIdAccessor;
    readonly CancellationToken _cancellationToken;
    readonly ILogger<ChronicleConnection> _logger;
    readonly ILoggerFactory _loggerFactory;
    readonly string? _certificatePath;
    readonly string? _certificatePassword;
    readonly ITokenProvider _tokenProvider;
    readonly bool _disableTls;
    GrpcChannel? _channel;
    IConnectionService? _connectionService;
    DateTimeOffset _lastKeepAlive = DateTimeOffset.MinValue;
    IServices _services;
    IDisposable? _keepAliveSubscription;
    TaskCompletionSource? _connectTcs;

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
    /// <param name="disableTls">Whether to disable TLS for the connection.</param>
    /// <param name="certificatePath">Optional path to the certificate file.</param>
    /// <param name="certificatePassword">Optional password for the certificate file.</param>
    /// <param name="tokenProvider"><see cref="ITokenProvider"/> for authentication.</param>
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
        bool disableTls,
        string? certificatePath = null,
        string? certificatePassword = null,
        ITokenProvider? tokenProvider = null)
    {
        _disableTls = disableTls;
        GrpcClientFactory.AllowUnencryptedHttp2 = _disableTls;
        _connectionString = connectionString;
        _connectTimeout = connectTimeout;
        _maxReceiveMessageSize = maxReceiveMessageSize;
        _maxSendMessageSize = maxSendMessageSize;
        Lifecycle = connectionLifecycle;
        _tasks = tasks;
        _correlationIdAccessor = correlationIdAccessor;
        _cancellationToken = cancellationToken;
        _logger = logger;
        _loggerFactory = loggerFactory;
        _certificatePath = certificatePath;
        _certificatePassword = certificatePassword;
        _tokenProvider = tokenProvider ?? new NoOpTokenProvider();

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
        _channel?.Dispose();
        _keepAliveSubscription?.Dispose();
        if (_tokenProvider is IDisposable disposableTokenProvider)
        {
            disposableTokenProvider.Dispose();
        }
    }

    /// <inheritdoc/>
    public async Task Connect()
    {
        if (Lifecycle.IsConnected)
        {
            return;
        }

        _logger.Connecting(_connectionString);
        _channel?.Dispose();
        _keepAliveSubscription?.Dispose();

        _channel = CreateGrpcChannel();
        var clientFactory = new InProcessAwareGrpcClientProxiesClientFactory();
        var callInvoker = _channel
            .Intercept(new AuthenticationClientInterceptor(_tokenProvider, _loggerFactory.CreateLogger<AuthenticationClientInterceptor>()))
            .Intercept(new CorrelationIdClientInterceptor(_correlationIdAccessor));

        // Perform compatibility check before establishing connection
        var tempConnectionService = callInvoker.CreateGrpcService<IConnectionService>(clientFactory);

        try
        {
            var serverSchemaResponse = await tempConnectionService.GetDescriptorSet();
            var clientSchema = CompatibilityValidator.GenerateClientSchema();
            var compatibilityResult = CompatibilityValidator.Validate(
                clientSchema,
                serverSchemaResponse.SchemaDefinition,
                _logger);

            if (!compatibilityResult.IsCompatible)
            {
                var errorMessage = string.Join("; ", compatibilityResult.Errors);
                _logger.IncompatibleWithServer(errorMessage);
                throw new InvalidOperationException($"Client is incompatible with server: {errorMessage}");
            }

            _logger.CompatibilityCheckPassed();
        }
        catch (InvalidOperationException)
        {
            throw;
        }
        catch (RpcException ex)
        {
            _logger.FailedToRetrieveServerDescriptorSet(ex.Message);

            // Don't fail the connection if we can't retrieve the schema
            // This allows backward compatibility with older servers that don't support this feature
        }

        _connectionService = callInvoker.CreateGrpcService<IConnectionService>(clientFactory);
        _lastKeepAlive = DateTimeOffset.UtcNow;
        _connectTcs = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

        _keepAliveSubscription = _connectionService.Connect(
            new()
            {
                ConnectionId = Lifecycle.ConnectionId,
                IsRunningWithDebugger = Debugger.IsAttached,
            }).Subscribe(HandleConnection);

        try
        {
            _services = new Services(
                callInvoker.CreateGrpcService<IEventStores>(clientFactory),
                callInvoker.CreateGrpcService<INamespaces>(clientFactory),
                callInvoker.CreateGrpcService<IRecommendations>(clientFactory),
                callInvoker.CreateGrpcService<IIdentities>(clientFactory),
                callInvoker.CreateGrpcService<IEventSequences>(clientFactory),
                callInvoker.CreateGrpcService<IEventTypes>(clientFactory),
                callInvoker.CreateGrpcService<IConstraints>(clientFactory),
                callInvoker.CreateGrpcService<IObservers>(clientFactory),
                callInvoker.CreateGrpcService<IFailedPartitions>(clientFactory),
                callInvoker.CreateGrpcService<IReactors>(clientFactory),
                callInvoker.CreateGrpcService<IReducers>(clientFactory),
                callInvoker.CreateGrpcService<IProjections>(clientFactory),
                callInvoker.CreateGrpcService<IWebhooks>(clientFactory),
                callInvoker.CreateGrpcService<IReadModels>(clientFactory),
                callInvoker.CreateGrpcService<IJobs>(clientFactory),
                callInvoker.CreateGrpcService<IEventSeeding>(clientFactory),
                callInvoker.CreateGrpcService<IUsers>(clientFactory),
                callInvoker.CreateGrpcService<IApplications>(clientFactory),
                callInvoker.CreateGrpcService<IServer>(clientFactory));

            await _connectTcs.Task.WaitAsync(TimeSpan.FromSeconds(_connectTimeout));
            _logger.Connected();
            await Lifecycle.Connected();
        }
        catch (TimeoutException)
        {
            _logger.TimedOut();
        }
        finally
        {
            StartWatchDog();
        }
    }

    void ConnectIfNotConnected()
    {
        if (!Lifecycle.IsConnected)
        {
            Connect().GetAwaiter().GetResult();
        }
    }

    GrpcChannel CreateGrpcChannel()
    {
        X509Certificate2? certificate = null;
        try
        {
#pragma warning disable CA2000 // Certificate ownership is transferred to httpHandler.SslOptions.ClientCertificates
            certificate = !_disableTls ? CertificateLoader.LoadCertificate(_certificatePath!, _certificatePassword!) : null;
            var httpHandler = new SocketsHttpHandler
            {
                PooledConnectionIdleTimeout = Timeout.InfiniteTimeSpan,
                KeepAlivePingDelay = TimeSpan.FromSeconds(60),
                KeepAlivePingTimeout = TimeSpan.FromSeconds(30),
                EnableMultipleHttp2Connections = true
            };

            if (!_disableTls && certificate is not null)
            {
                httpHandler.SslOptions.ClientCertificates = new X509CertificateCollection { certificate };
                _logger.UsingClientCertificate(_certificatePath!);
            }
#pragma warning restore CA2000

            if (!_disableTls)
            {
                var certHashString = certificate?.GetCertHashString();
                httpHandler.SslOptions.RemoteCertificateValidationCallback = (sender, cert, chain, sslPolicyErrors) =>
                {
                    if (sslPolicyErrors == SslPolicyErrors.None)
                    {
                        return true;
                    }

                    if (cert is not null && certHashString is not null)
                    {
                        return cert.GetCertHashString() == certHashString;
                    }

                    // For development: accept localhost certificates with name mismatches
                    return sslPolicyErrors == SslPolicyErrors.RemoteCertificateNameMismatch;
                };
            }

            var scheme = _disableTls ? "http" : "https";
            var address = $"{scheme}://{_connectionString.ServerAddress.Host}:{_connectionString.ServerAddress.Port}";

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
        _lastKeepAlive = DateTimeOffset.UtcNow;

        if (!Debugger.IsAttached)
        {
            _connectionService?.ConnectionKeepAlive(keepAlive).GetAwaiter().GetResult();
        }
    }

    void StartWatchDog()
    {
        _ = _tasks.Run(
            async () =>
            {
                while (!_cancellationToken.IsCancellationRequested)
                {
                    await _tasks.Delay(1000, _cancellationToken);
                    var delta = DateTimeOffset.UtcNow.Subtract(_lastKeepAlive);
                    if (delta.TotalSeconds > 5)
                    {
                        break;
                    }
                }

                if (_connectTcs?.Task.IsCompleted == true)
                {
                    _logger.Disconnected();
                    await Lifecycle.Disconnected();
                }
                _logger.Reconnecting();
                await Connect();
            },
            _cancellationToken);
    }
}
