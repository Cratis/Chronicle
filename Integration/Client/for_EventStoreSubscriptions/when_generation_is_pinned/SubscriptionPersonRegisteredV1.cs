// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Compliance.GDPR;
using Cratis.Chronicle.Events;

namespace Cratis.Chronicle.Integration.for_EventStoreSubscriptions.when_generation_is_pinned;

/// <summary>
/// Records the previous public person registration contract.
/// </summary>
/// <param name="Name">The registered person's name.</param>
[EventTypeGenerationFor<SubscriptionPersonRegistered>(1)]
[Public]
public record SubscriptionPersonRegisteredV1([property: PII] string Name);
