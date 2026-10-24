// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;

namespace Cratis.Chronicle.EventStores;

/// <summary>
/// The exception that is thrown when deployment registration cannot record its event.
/// </summary>
/// <param name="eventStore">The event store being registered.</param>
public class EventStoreDeploymentRegistrationFailed(EventStoreName eventStore) : Exception($"Could not record deployment registration for event store '{eventStore}'.");
