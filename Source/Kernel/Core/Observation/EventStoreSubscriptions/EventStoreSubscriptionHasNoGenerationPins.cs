// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Observation.EventStoreSubscriptions;

/// <summary>
/// The exception that is thrown when a pinned subscription has no exact generation declarations.
/// </summary>
public class EventStoreSubscriptionHasNoGenerationPins() : Exception("A pinned event store subscription must declare at least one exact event generation.");
