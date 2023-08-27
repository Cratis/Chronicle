// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Aksio.Cratis.Connections;

namespace Aksio.Cratis.Client;

/// <summary>
/// Represents an implementation of <see cref="IMultiTenantClient"/>.
/// </summary>
public class MultiTenantClient : Client, IMultiTenantClient
{
    /// <summary>
    /// Initializes a new instance of the <see cref="SingleTenantClient"/> class.
    /// </summary>
    /// <param name="eventStore">The <see cref="IMultiTenantEventStore"/>.</param>
    /// <param name="connection">The <see cref="IConnection"/> to connect to Cratis.</param>
    /// <param name="serviceProvider"><see cref="IServiceProvider"/> for getting instances of services.</param>
    public MultiTenantClient(IMultiTenantEventStore eventStore, IConnection connection, IServiceProvider serviceProvider)
        : base(connection, serviceProvider, true)
    {
        EventStore = eventStore;
    }

    /// <inheritdoc/>
    public IMultiTenantEventStore EventStore { get; }
}
