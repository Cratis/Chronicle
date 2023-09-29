// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Aksio.Cratis.Kernel.Contracts.EventSequences;
using Grpc.Net.Client;
using ProtoBuf.Grpc.Client;

namespace Aksio.Cratis;

/// <summary>
/// Represents an implementation of <see cref="ICratisClient"/>.
/// </summary>
public class CratisClient : ICratisClient, IDisposable
{
    readonly CratisSettings _settings;
    readonly GrpcChannel _channel;
    readonly IEventSequences _eventSequences;

    /// <summary>
    /// Initializes a new instance of the <see cref="CratisClient"/> class.
    /// </summary>
    /// <param name="connectionString">Connection string to use.</param>
    public CratisClient(string connectionString)
        : this(new CratisUrl(connectionString))
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="CratisClient"/> class.
    /// </summary>
    /// <param name="url"><see cref="CratisUrl"/> to connect with.</param>
    public CratisClient(CratisUrl url)
        : this(CratisSettings.FromUrl(url))
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="CratisClient"/> class.
    /// </summary>
    /// <param name="settings"><see cref="CratisSettings"/> to use.</param>
    public CratisClient(CratisSettings settings)
    {
        _settings = settings;
        GrpcClientFactory.AllowUnencryptedHttp2 = true;
        _channel = GrpcChannel.ForAddress("http://localhost:35000");
        _eventSequences = _channel.CreateGrpcService<IEventSequences>();
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        _channel.Dispose();
    }

    /// <inheritdoc/>
    public IEventStore GetEventStore(EventStoreName name, TenantId? tenantId = null) => throw new NotImplementedException();

    /// <inheritdoc/>
    public IAsyncEnumerable<EventStoreName> ListEventStores(CancellationToken cancellationToken = default) => throw new NotImplementedException();
}
