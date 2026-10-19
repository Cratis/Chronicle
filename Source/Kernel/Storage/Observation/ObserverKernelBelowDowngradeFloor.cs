// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Observation;

namespace Cratis.Chronicle.Storage.Observation;

/// <summary>
/// The exception that is thrown when a kernel below the gate release attempts to activate an initialized observer.
/// </summary>
/// <param name="eventStore">The event store containing initialized state.</param>
/// <param name="observerId">The observer whose activation must be refused.</param>
public class ObserverKernelBelowDowngradeFloor(EventStoreName eventStore, ObserverId observerId)
    : Exception($"Cannot activate observer '{observerId}' in event store '{eventStore}': this kernel is below the observer downgrade floor. Initialized observers require the gate release or later (decision 0011).");
