// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Aksio.Cratis.Observation;

/// <summary>
/// Represents the registration of all observers for a client.
/// </summary>
/// <param name="Registrations">Collection of <see cref="ClientObserverRegistration"/>.</param>
public record ObserversRegistration(IEnumerable<ClientObserverRegistration> Registrations);
