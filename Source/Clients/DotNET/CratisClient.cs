// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Aksio.Cratis.Auditing;
using Aksio.Cratis.Connections;
using Aksio.Cratis.Events;
using Aksio.Cratis.Identities;
using Microsoft.Extensions.Logging;

namespace Aksio.Cratis;

/// <summary>
/// Represents an implementation of <see cref="ICratisClient"/>.
/// </summary>
public class CratisClient : ICratisClient, IDisposable
{
    readonly CratisSettings _settings;
    readonly ICratisConnection? _connection;
    readonly IEventTypes _eventTypes;
    readonly IEventSerializer _eventSerializer;
    readonly ICausationManager _causationManager;

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

        _eventTypes = new EventTypes(_settings.ArtifactsProvider);
        _eventSerializer = new EventSerializer(
            settings.ArtifactsProvider,
            settings.ServiceProvider,
            _eventTypes,
            settings.JsonSerializerOptions);
        _causationManager = new CausationManager();

        _connection = new CratisConnection(
            _settings,
            new ConnectionLifecycle(
                new IParticipateInConnectionLifecycle[]
                {
                },
                settings.LoggerFactory.CreateLogger<ConnectionLifecycle>()),
            new Tasks.Tasks(),
            CancellationToken.None);
    }

    /// <inheritdoc/>
    public void Dispose()
    {
    }

    /// <inheritdoc/>
    public IEventStore GetEventStore(EventStoreName name, TenantId? tenantId = null) =>
        new EventStore(
            name,
            tenantId ?? TenantId.Development,
            _connection!,
            _eventTypes,
            _eventSerializer,
            _causationManager,
            _settings.IdentityProvider);

    /// <inheritdoc/>
    public IAsyncEnumerable<EventStoreName> ListEventStores(CancellationToken cancellationToken = default) => throw new NotImplementedException();
}
