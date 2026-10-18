// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;

namespace Cratis.Chronicle.Storage.Observation;

/// <summary>
/// A separately persisted, versioned obligation or progress record. Terminal rows are absorbing until explicitly deleted.
/// </summary>
/// <typeparam name="T">The row's payload.</typeparam>
/// <param name="Key">The immutable row identity.</param>
/// <param name="Version">The compare-and-swap version, starting at zero.</param>
/// <param name="State">The forward-only lifecycle state.</param>
/// <param name="Data">The payload stored atomically with its state.</param>
/// <param name="Position">The optional durable progress or continuation position; it can only advance.</param>
public record ObserverDefinitionChangeRow<T>(
    ObserverDefinitionChangeRowKey Key,
    long Version,
    ObserverDefinitionChangeRowState State,
    T Data,
    EventSequenceNumber? Position = null);
