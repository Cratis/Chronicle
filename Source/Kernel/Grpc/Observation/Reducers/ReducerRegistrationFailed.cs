// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Clients;
using Cratis.Chronicle.Concepts.EventSequences;
using Cratis.Chronicle.Concepts.Observation.Reducers;

namespace Cratis.Chronicle.Services.Observation.Reducers;

/// <summary>
/// The exception that is thrown when reducer registration fails.
/// </summary>
/// <param name="reducerId">The reducer identifier.</param>
/// <param name="eventStore">The event store where registration failed.</param>
/// <param name="eventStoreNamespace">The namespace where registration failed.</param>
/// <param name="eventSequenceId">The event sequence used by the reducer.</param>
/// <param name="connectionId">The client connection identifier.</param>
/// <param name="innerException">The underlying cause.</param>
public class ReducerRegistrationFailed(
    ReducerId reducerId,
    EventStoreName eventStore,
    EventStoreNamespaceName eventStoreNamespace,
    EventSequenceId eventSequenceId,
    ConnectionId connectionId,
    Exception innerException)
    : Exception($"Failed to register reducer '{reducerId}' for event store '{eventStore}', namespace '{eventStoreNamespace}', event sequence '{eventSequenceId}', connection '{connectionId}'. Root cause: {innerException.GetBaseException().Message}", innerException);
