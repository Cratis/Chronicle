// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.Generations.Bindings;

/// <summary>
/// Defines who owns an observer identity within an event store.
/// </summary>
public enum ObserverIdentityClaimant
{
    /// <summary>
    /// A user or legacy observer definition owns the identity.
    /// </summary>
    Registration = 0,

    /// <summary>
    /// A later materialization generation owns the identity.
    /// </summary>
    Generation = 1
}
