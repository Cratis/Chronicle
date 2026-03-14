// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
using System.Text.Json;
using System.Text.Json.Serialization;
using Cratis.Chronicle.Connections;
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
using Cratis.Json;
using Grpc.Core.Interceptors;
using Grpc.Net.Client;
using Grpc.Net.Client.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using ProtoBuf.Grpc.Client;
using StatusCode = Grpc.Core.StatusCode;

namespace Cratis.Chronicle.Cli;

/// <summary>
/// Lightweight gRPC client for CLI usage. Connects directly to the Chronicle server
/// without the overhead of the full ChronicleClient (no artifact scanning, no keep-alive,
/// no event store lifecycle management).
/// </summary>
public sealed class CliServiceClient : IDisposable
{
    readonly GrpcChannel _channel;
    readonly ITokenProvider? _tokenProvider;

    CliServiceClient(GrpcChannel channel, IServices services, ITokenProvider? tokenProvider)
    {
        _channel = channel;
        _tokenProvider = tokenProvider;
        Services = services;
    }

    /// <summary>
    /// Gets the JSON serializer options configured for CLI output.
    /// </summary>
    public static JsonSerializerOptions JsonSerializerOptions { get; } = CreateJsonSerializerOptions();

    /// <summary>
    /// Gets the gRPC service proxies.
    /// </summary>
    public IServices Services { get; }

    /// <summary>
    /// Creates a new <see cref="CliServiceClient"/> from a connection string.
    /// </summary>
    /// <param name="connectionString">The parsed Chronicle connection string.</param>
    /// <param name="managementPort">The management port for the token endpoint.</param>
    /// <returns>A connected <see cref="CliServiceClient"/>.</returns>
    public static CliServiceClient Create(ChronicleConnectionString connectionString, int managementPort)
    {
        var disableTls = connectionString.DisableTls;
        GrpcClientFactory.AllowUnencryptedHttp2 = disableTls;

        var scheme = disableTls ? "http" : "https";
        var address = $"{scheme}://{connectionString.ServerAddress.Host}:{connectionString.ServerAddress.Port}";

        X509Certificate2? certificate = null;
        if (!disableTls && !string.IsNullOrEmpty(connectionString.CertificatePath))
        {
#pragma warning disable CA2000 // Certificate ownership is transferred to httpHandler.SslOptions.ClientCertificates
            certificate = CertificateLoader.LoadCertificate(connectionString.CertificatePath, connectionString.CertificatePassword ?? string.Empty);
#pragma warning restore CA2000
        }

        var httpHandler = new SocketsHttpHandler
        {
            PooledConnectionIdleTimeout = Timeout.InfiniteTimeSpan,
            EnableMultipleHttp2Connections = true
        };

        if (!disableTls)
        {
            if (certificate is not null)
            {
                httpHandler.SslOptions.ClientCertificates = new X509CertificateCollection { certificate };
            }

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

                return sslPolicyErrors == SslPolicyErrors.RemoteCertificateNameMismatch;
            };
        }

        var channel = GrpcChannel.ForAddress(
            address,
            new GrpcChannelOptions
            {
                HttpHandler = httpHandler,
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

        ITokenProvider? tokenProvider = null;
        var callInvoker = channel.CreateCallInvoker();

        if (connectionString.AuthenticationMode == AuthenticationMode.ClientCredentials)
        {
            tokenProvider = new OAuthTokenProvider(
                connectionString.ServerAddress,
                connectionString.Username ?? string.Empty,
                connectionString.Password ?? string.Empty,
                managementPort,
                disableTls,
                NullLoggerFactory.Instance.CreateLogger<OAuthTokenProvider>());

            callInvoker = callInvoker
                .Intercept(new AuthenticationClientInterceptor(
                    tokenProvider,
                    NullLoggerFactory.Instance.CreateLogger<AuthenticationClientInterceptor>()));
        }

        var clientFactory = new InProcessAwareGrpcClientProxiesClientFactory();
        var services = new Services(
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

        return new CliServiceClient(channel, services, tokenProvider);
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        if (_tokenProvider is IDisposable disposable)
        {
            disposable.Dispose();
        }

        _channel.Dispose();
    }

    static JsonSerializerOptions CreateJsonSerializerOptions()
    {
        var options = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        };
        options.Converters.Add(new EnumConverterFactory());
        options.Converters.Add(new ConceptAsJsonConverterFactory());
        return options;
    }
}
