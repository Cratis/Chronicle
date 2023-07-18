// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.


using Aksio.Cratis.Clients;

namespace Aksio.Cratis.Client;

/// <summary>
/// Represents an implementation of <see cref="ISingleTenantClient"/>.
/// </summary>
public class SingleTenantClient : CratisClient, ISingleTenantClient
{
    /// <inheritdoc/>
    public ISingleTenantEventStore EventStore { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="SingleTenantClient"/> class.
    /// </summary>
    /// <param name="eventStore">The <see cref="ISingleTenantEventStore"/>.</param>
    /// <param name="client">The <see cref="IClient"/> to connect to Cratis.</param>
    public SingleTenantClient(ISingleTenantEventStore eventStore, IClient client)
        : base(client)
    {
        EventStore = eventStore;
    }
}
