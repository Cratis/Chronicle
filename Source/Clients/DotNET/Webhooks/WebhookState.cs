// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Cratis.Chronicle.Observation;

namespace Cratis.Chronicle.Webhooks;

/// <summary>
/// Represents the state of a webhook.
/// </summary>
/// <param name="RunningState">The current running state of the webhook.</param>
/// <param name="IsSubscribed">Indicates whether the webhook is subscribed.</param>
/// <param name="NextEventSequenceNumber">The next event sequence number.</param>
/// <param name="LastHandledEventSequenceNumber">The last handled event sequence number.</param>
public record WebhookState(
    ObserverRunningState RunningState,
    bool IsSubscribed,
    EventSequenceNumber NextEventSequenceNumber,
    EventSequenceNumber LastHandledEventSequenceNumber);
