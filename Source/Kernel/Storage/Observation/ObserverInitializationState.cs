// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.Observation;

/// <summary>
/// The persisted enablement state of a namespace observer under decisions 0010 and 0011.
/// </summary>
public enum ObserverInitializationState
{
    /// <summary>
    /// The observer retains legacy behavior, including after a refused initialization attempt.
    /// </summary>
    NotInitialized = 0,

    /// <summary>
    /// Initialization awaits a registration that can supply the namespace's baseline.
    /// </summary>
    AwaitingRegistration = 1,

    /// <summary>
    /// Legacy work is being drained and its pending handovers have been given up with consent.
    /// </summary>
    Initializing = 2,

    /// <summary>
    /// The observer has initialized its type-set control from its applied registration.
    /// </summary>
    Initialized = 3
}
