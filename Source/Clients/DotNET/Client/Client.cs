// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Aksio.Cratis.Connections;
using Microsoft.Extensions.DependencyInjection;

namespace Aksio.Cratis.Client;

/// <summary>
/// Represents an implementation of <see cref="IClient"/>.
/// </summary>
public class Client : IClient
{
    readonly IConnection _connection;
    readonly IServiceProvider _serviceProvider;

    /// <inheritdoc/>
    public bool IsMultiTenanted { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="Client"/> class.
    /// </summary>
    /// <param name="connection"><see cref="IConnection"/> to use.</param>
    /// <param name="serviceProvider"><see cref="IServiceProvider"/> for getting instances of services.</param>
    /// <param name="isMultiTenanted">Whether or not it is a multi tenanted client.</param>
    public Client(IConnection connection, IServiceProvider serviceProvider, bool isMultiTenanted = false)
    {
        _connection = connection;
        _serviceProvider = serviceProvider;
        IsMultiTenanted = isMultiTenanted;
    }

    /// <inheritdoc/>
    public Task Connect()
    {
        return _connection.Connect();
    }

    /// <inheritdoc/>
    public IEventSequences GetEventSequences(TenantId? tenantId = default)
    {
        if (!IsMultiTenanted) return _serviceProvider.GetRequiredService<IEventSequences>();
        if (tenantId is null)
        {
            throw new TenantIsRequired("getting event sequences");
        }

        return _serviceProvider.GetRequiredService<IMultiTenantEventSequences>().ForTenant(tenantId);
    }
}
