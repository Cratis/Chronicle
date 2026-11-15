// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Observation.EventStoreSubscriptions;

/// <summary>
/// Represents a named tag of an event store subscription filter, as persisted in the subscription events.
/// </summary>
/// <param name="Name">The tag name.</param>
/// <param name="Value">The tag value.</param>
public record EventStoreSubscriptionTag(string Name, string Value);
