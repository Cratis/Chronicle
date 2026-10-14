// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.EventSequences.Concurrency;

namespace Cratis.Chronicle.Transactions;

/// <summary>
/// A stream or aggregate read's exact dependency, enrolled before exposing its value.
/// </summary>
/// <param name="Origin">Explicit or aggregate read provenance.</param>
/// <param name="Label">The commit label.</param>
/// <param name="Scope">The captured condition.</param>
/// <param name="EventStore">The target store.</param>
/// <param name="Namespace">The target namespace.</param>
/// <param name="EventSequenceId">The target sequence.</param>
/// <param name="Description">The read description.</param>
public record ContributedRead(ConcurrencyScopeOrigin Origin, EventSourceId Label, ConcurrencyScope Scope, EventStoreName EventStore, EventStoreNamespaceName Namespace, EventSequenceId EventSequenceId, string Description);
