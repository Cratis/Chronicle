// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Concurrent;
using Cratis.Chronicle.Auditing;
using Cratis.Chronicle.Connections;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.Identities;
using Cratis.Chronicle.Observation;

namespace Cratis.Chronicle.Client;

/// <summary>
/// Represents an implementation of <see cref="IMultiTenantEventSequences"/>.
/// </summary>
public class MultiTenantEventSequences : IMultiTenantEventSequences
{
    readonly ConcurrentDictionary<TenantId, IEventSequences> _sequences = new();
    readonly IConnection _connection;
    readonly IObserversRegistrar _observersRegistrar;
    readonly IEventTypes _eventTypes;
    readonly IEventSerializer _eventSerializer;
    readonly ICausationManager _causationManager;
    readonly IIdentityProvider _identityProvider;
    readonly IExecutionContextManager _executionContextManager;

    /// <summary>
    /// Initializes a new instance of the <see cref="SingleTenantEventStore"/> class.
    /// </summary>
    /// <param name="connection">The <see cref="IConnection"/> to use.</param>
    /// <param name="observersRegistrar"><see cref="IObserversRegistrar"/> to use.</param>
    /// <param name="eventTypes">Known <see cref="IEventTypes"/>.</param>
    /// <param name="eventSerializer">The <see cref="IEventSerializer"/> for serializing events.</param>
    /// <param name="causationManager"><see cref="ICausationManager"/> for getting causation.</param>
    /// <param name="identityProvider"><see cref="IIdentityProvider"/> for resolving identity for operations.</param>
    /// <param name="executionContextManager"><see cref="IExecutionContextManager"/> for working with the execution context.</param>
    public MultiTenantEventSequences(
        IConnection connection,
        IObserversRegistrar observersRegistrar,
        IEventTypes eventTypes,
        IEventSerializer eventSerializer,
        ICausationManager causationManager,
        IIdentityProvider identityProvider,
        IExecutionContextManager executionContextManager)
    {
        _connection = connection;
        _observersRegistrar = observersRegistrar;
        _eventTypes = eventTypes;
        _eventSerializer = eventSerializer;
        _causationManager = causationManager;
        _identityProvider = identityProvider;
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
                _connection,
                _observersRegistrar,
                _causationManager,
                _identityProvider,
                _executionContextManager);
            _sequences.TryAdd(tenantId, sequences);
        }

        return sequences;
    }
}
