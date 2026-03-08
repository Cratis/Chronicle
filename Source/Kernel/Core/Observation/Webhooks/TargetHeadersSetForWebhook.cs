// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;

namespace Cratis.Chronicle.Observation.Webhooks;

/// <summary>
/// Represents the event for target headers being set for a webhook.
/// </summary>
/// <param name="TargetHeaders">The target headers.</param>
[EventType]
public record TargetHeadersSetForWebhook(IReadOnlyDictionary<string, string> TargetHeaders);
