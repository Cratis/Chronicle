// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Compliance.GDPR;
using Cratis.Chronicle.Events;

namespace Cratis.Chronicle.Integration.for_EventStoreSubscriptions.when_generation_is_pinned;

/// <summary>
/// Records the public person registration contract used by the pinned subscription specifications.
/// </summary>
/// <param name="FullName">The registered person's full name.</param>
[EventType("subscription-person-registered", generation: 2)]
[Public]
public record SubscriptionPersonRegistered([property: PII] string FullName);
