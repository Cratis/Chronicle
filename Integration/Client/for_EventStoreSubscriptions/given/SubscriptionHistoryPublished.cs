// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;

namespace Cratis.Chronicle.Integration.for_EventStoreSubscriptions.given;

/// <summary>
/// Represents a distinguishable retained public event for subscription lifecycle specifications.
/// </summary>
/// <param name="Value">The value identifying this publication.</param>
[EventType("subscription-history-published")]
[Public]
public record SubscriptionHistoryPublished(string Value);
