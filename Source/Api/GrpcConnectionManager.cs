// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Grpc.Core;
using Grpc.Net.Client;
using Grpc.Net.Client.Configuration;
using Microsoft.Extensions.Options;

namespace Cratis.Chronicle.Api;

/// <summary>
/// Represents a manager for the gRPC connection.
/// </summary>
public class GrpcConnectionManager
{
    readonly IOptions<ChronicleApiOptions> _options;

    /// <summary>
    /// Initializes a new instance of the <see cref="GrpcConnectionManager"/> class.
    /// </summary>
    /// <param name="options"><see cref="ChronicleApiOptions"/> instance.</param>
    public GrpcConnectionManager(IOptions<ChronicleApiOptions> options)
    {
        _options = options;
        Channel = CreateGrpcChannel();
    }

    /// <summary>
    /// Gets the <see cref="GrpcChannel"/>.
    /// </summary>
    public GrpcChannel Channel { get; }

    GrpcChannel CreateGrpcChannel()
    {
        var httpHandler = new SocketsHttpHandler
        {
            // ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator
            PooledConnectionIdleTimeout = Timeout.InfiniteTimeSpan,
            KeepAlivePingDelay = TimeSpan.FromSeconds(60),
            KeepAlivePingTimeout = TimeSpan.FromSeconds(30),
            EnableMultipleHttp2Connections = true
        };

        var address = _options.Value.KernelUrl;
        return GrpcChannel.ForAddress(
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
    }
}
