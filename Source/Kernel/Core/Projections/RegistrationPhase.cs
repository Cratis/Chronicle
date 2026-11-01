// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Projections;

/// <summary>
/// The phases a projection registration passes through, in order.
/// </summary>
enum RegistrationPhase
{
    /// <summary>Detecting which definitions changed.</summary>
    Detecting = 0,

    /// <summary>Registering with the projection engine.</summary>
    Engine = 1,

    /// <summary>Subscribing the observers.</summary>
    Subscribing = 2,

    /// <summary>Retiring projections that are no longer registered.</summary>
    Retiring = 3
}
