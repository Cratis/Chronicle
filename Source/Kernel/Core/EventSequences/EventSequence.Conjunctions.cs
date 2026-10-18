// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Auditing;
using Cratis.Chronicle.Concepts.EventSequences.Concurrency;
using Cratis.Chronicle.Concepts.Identities;

namespace Cratis.Chronicle.EventSequences;

public partial class EventSequence
{
    /// <inheritdoc/>
    public Task<AppendManyResult> AppendManyWithConjunctions(
        IEnumerable<EventToAppend> events,
        CorrelationId correlationId,
        IEnumerable<Causation> causation,
        Identity causedBy,
        ConcurrencyScopeConjunctions concurrencyScopes) =>
        AppendManyValidated(events, correlationId, causation, causedBy, concurrencyScopes);

    /// <inheritdoc/>
    public Task<AppendManyResult> AppendManyWithConjunctionsAndEffectReceipt(
        IEnumerable<EventToAppend> events,
        CorrelationId correlationId,
        IEnumerable<Causation> causation,
        Identity causedBy,
        ConcurrencyScopeConjunctions concurrencyScopes,
        Occurrences.EffectReceipt effectReceipt) =>
        AppendManyValidated(events, correlationId, causation, causedBy, concurrencyScopes, effectReceipt);
}
