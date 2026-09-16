// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;

namespace Cratis.Chronicle.EventSequences.for_EventSequence.given;

public class an_event_sequence : all_dependencies
{
    protected readonly EventStoreName _eventStoreName = Guid.NewGuid().ToString();
    protected readonly EventStoreNamespaceName _namespace = Guid.NewGuid().ToString();
    protected EventSequence _eventSequence;

    void Establish()
    {
        _eventSequence = new(
            _eventStoreName,
            _namespace,
            Guid.NewGuid().ToString(),
            _connection,
            _eventTypes,
            _constraints,
            _eventSerializer,
            _correlationIdAccessor,
            _concurrencyScopeStrategies,
            _causationManager,
            _unitOfWorkManager,
            _identityProvider,
            JsonSerializerOptions.Default);
    }
}
