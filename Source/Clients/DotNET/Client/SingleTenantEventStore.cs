// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Aksio.Cratis.Clients;
using Aksio.Cratis.Events;
using Aksio.Cratis.Observation;
using Microsoft.Extensions.DependencyInjection;

namespace Aksio.Cratis.Client;

/// <summary>
/// Represents an implementation of <see cref="ISingleTenantEventStore"/>.
/// </summary>
public class SingleTenantEventStore : ISingleTenantEventStore
{
    readonly IServiceProvider _serviceProvider;
    readonly IEventTypes _eventTypes;
    readonly IEventSerializer _eventSerializer;
    readonly IExecutionContextManager _executionContextManager;

    IEventSequences? _sequences;

    /// <inheritdoc/>
    public IEventSequences Sequences
    {
        get
        {
            _sequences ??= new EventSequences(
                TenantId.Development,
                _eventTypes,
                _eventSerializer,
                _serviceProvider.GetRequiredService<IClient>(),
                _serviceProvider.GetRequiredService<IObserversRegistrar>(),
                _executionContextManager);
            return _sequences;
        }
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="SingleTenantEventStore"/> class.
    /// </summary>
    /// <param name="serviceProvider"><see cref="IServiceProvider"/> for resolving services.</param>
    /// <param name="eventTypes">Known <see cref="IEventTypes"/>.</param>
    /// <param name="eventSerializer">The <see cref="IEventSerializer"/> for serializing events.</param>
    /// <param name="executionContextManager"><see cref="IExecutionContextManager"/> for working with the execution context.</param>
    public SingleTenantEventStore(
        IServiceProvider serviceProvider,
        IEventTypes eventTypes,
        IEventSerializer eventSerializer,
        IExecutionContextManager executionContextManager)
    {
        _serviceProvider = serviceProvider;
        _eventTypes = eventTypes;
        _eventSerializer = eventSerializer;
        _executionContextManager = executionContextManager;
    }
}
