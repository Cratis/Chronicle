// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Orleans.Jobs;

namespace Cratis.Chronicle.Storage.Observation;

/// <summary>
/// A replay successor with finite bounds and an explicit scope. A null partition means the whole observer.
/// </summary>
/// <param name="JobId">The fresh job id, never an execution that may already exist.</param>
/// <param name="Partition">The partition, or null for a full replay.</param>
/// <param name="From">The inclusive lower bound.</param>
/// <param name="To">The inclusive upper bound.</param>
/// <param name="Scope">The requested event types, reconciled with the new members by the writer.</param>
public record ObserverRestartPlan(JobId JobId, Key? Partition, EventSequenceNumber From, EventSequenceNumber To, ObserverReaderScope Scope);
