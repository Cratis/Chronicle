// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Aksio.Cratis.Events;
using Aksio.DependencyInversion;

namespace Aksio.Cratis.Client;

/// <summary>
/// Represents an implementation of <see cref="IMultiTenantClient"/>.
/// </summary>
public class MultiTenantClient : IMultiTenantClient
{
    /// <inheritdoc/>
    public IMultiTenantEventStore EventStore { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="SingleTenantClient"/> class.
    /// </summary>
    /// <param name="serviceProvider">Provider for <see cref="IServiceProvider"/> for resolving services.</param>
    /// <param name="eventTypes">Known <see cref="IEventTypes"/>.</param>
    /// <param name="eventSerializer">The <see cref="IEventSerializer"/> for serializing events.</param>
    /// <param name="executionContextManager"><see cref="IExecutionContextManager"/> for working with the execution context.</param>
    public MultiTenantClient(
        ProviderFor<IServiceProvider> serviceProvider,
        IEventTypes eventTypes,
        IEventSerializer eventSerializer,
        IExecutionContextManager executionContextManager)
    {
        EventStore = new MultiTenantEventStore(serviceProvider, eventTypes, eventSerializer, executionContextManager);
    }
}
