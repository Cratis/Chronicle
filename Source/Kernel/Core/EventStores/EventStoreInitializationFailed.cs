// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;

namespace Cratis.Chronicle.EventStores;

/// <summary>
/// The exception that is thrown when store initialization cannot record its notification.
/// </summary>
/// <param name="eventStore">The event store being initialized.</param>
public class EventStoreInitializationFailed(EventStoreName eventStore) : Exception($"Could not record initialization of event store '{eventStore}'.");
