// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Aksio.Cratis.Clients;

namespace Aksio.Cratis.Client;

/// <summary>
/// Represents an implementation of <see cref="IMultiTenantClient"/>.
/// </summary>
public class MultiTenantClient : CratisClient, IMultiTenantClient
{
    /// <inheritdoc/>
    public IMultiTenantEventStore EventStore { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="SingleTenantClient"/> class.
    /// </summary>
    /// <param name="eventStore">The <see cref="IMultiTenantEventStore"/>.</param>
    /// <param name="client">The <see cref="IClient"/> to connect to Cratis.</param>
    public MultiTenantClient(IMultiTenantEventStore eventStore, IClient client)
        : base(client)
    {
        EventStore = eventStore;
    }
}
