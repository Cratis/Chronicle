// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;

namespace Cratis.Chronicle.Chaos;

/// <summary>
/// An immutable ledger intention appended by the chaos producer.
/// </summary>
/// <param name="LedgerId">Stable idempotency identity.</param>
/// <param name="Timestamp">Original intention timestamp, retained across retries.</param>
/// <param name="Payload">The numeric contribution.</param>
[EventType("chronicle-chaos-event")]
public sealed record ChaosEvent(string LedgerId, DateTimeOffset Timestamp, long Payload);
