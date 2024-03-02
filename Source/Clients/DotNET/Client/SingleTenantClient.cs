// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Aksio.Cratis.Connections;

namespace Aksio.Cratis.Client;

/// <summary>
/// Represents an implementation of <see cref="ISingleTenantClient"/>.
/// </summary>
public class SingleTenantClient : Client, ISingleTenantClient
{
    /// <summary>
    /// Initializes a new instance of the <see cref="SingleTenantClient"/> class.
    /// </summary>
    /// <param name="eventStore">The <see cref="ISingleTenantEventStore"/>.</param>
    /// <param name="connection">The <see cref="IConnection"/> to connect to Cratis.</param>
    /// <param name="serviceProvider"><see cref="IServiceProvider"/> for getting instances of services.</param>
    public SingleTenantClient(ISingleTenantEventStore eventStore, IConnection connection, IServiceProvider serviceProvider)
        : base(connection, serviceProvider)
    {
        EventStore = eventStore;
    }

    /// <inheritdoc/>
    public ISingleTenantEventStore EventStore { get; }
}
