// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Concurrent;
using Aksio.Cratis.Clients;
using Aksio.Cratis.Events;
using Aksio.Cratis.Observation;

namespace Aksio.Cratis.Client;

/// <summary>
/// Represents an implementation of <see cref="IMultiTenantEventSequences"/>.
/// </summary>
public class MultiTenantEventSequences : IMultiTenantEventSequences
{
    readonly ConcurrentDictionary<TenantId, IEventSequences> _sequences = new();
    readonly IClient _client;
    readonly IObserversRegistrar _observersRegistrar;
    readonly IEventTypes _eventTypes;
    readonly IEventSerializer _eventSerializer;
    readonly IExecutionContextManager _executionContextManager;

    /// <summary>
    /// Initializes a new instance of the <see cref="SingleTenantEventStore"/> class.
    /// </summary>
    /// <param name="client">The <see cref="IClient"/> to use.</param>
    /// <param name="observersRegistrar"><see cref="IObserversRegistrar"/> to use.</param>
    /// <param name="eventTypes">Known <see cref="IEventTypes"/>.</param>
    /// <param name="eventSerializer">The <see cref="IEventSerializer"/> for serializing events.</param>
    /// <param name="executionContextManager"><see cref="IExecutionContextManager"/> for working with the execution context.</param>
    public MultiTenantEventSequences(
        IClient client,
        IObserversRegistrar observersRegistrar,
        IEventTypes eventTypes,
        IEventSerializer eventSerializer,
        IExecutionContextManager executionContextManager)
    {
        _client = client;
        _observersRegistrar = observersRegistrar;
        _eventTypes = eventTypes;
        _eventSerializer = eventSerializer;
        _executionContextManager = executionContextManager;
    }

    /// <inheritdoc/>
    public IEventSequences ForTenant(TenantId tenantId)
    {
        if (!_sequences.TryGetValue(tenantId, out var sequences))
        {
            sequences = new EventSequences(
                tenantId,
                _eventTypes,
                _eventSerializer,
                _client,
                _observersRegistrar,
                _executionContextManager);
            _sequences.TryAdd(tenantId, sequences);
        }

        return sequences;
    }
}
