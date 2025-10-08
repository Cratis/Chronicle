// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.EventSequences;

namespace Cratis.Chronicle.Webhooks;

/// <summary>
/// Optional attribute used to adorn classes to configure a webhook. The webhook will have to implement <see cref="Webhook"/>.
/// </summary>
/// <remarks>
/// Initializes a new instance of <see cref="WebhookAttribute"/>.
/// </remarks>
/// <param name="id">Optional identifier. If not specified, it will default to the fully qualified type name.</param>
/// <param name="eventSequence">Optional the name of the event sequence to observe. Defaults to the event log.</param>
/// <param name="isActive">Optional whether the webhook is active or not. If it's passive, it won't run actively on the Kernel.</param>
/// <param name="isReplayable">Optional whether the webhook can be replayed or not. If it's not replayable it will not be replayed when the definition changes.</param>
[AttributeUsage(AttributeTargets.Class)]
public sealed class WebhookAttribute(string id = "", string? eventSequence = default, bool isActive = true, bool isReplayable = true) : Attribute
{
    /// <summary>
    /// Gets the unique identifier for the webhook.
    /// </summary>
    public WebhookId Id { get; } = id;

    /// <summary>
    /// Gets the unique identifier for an event sequence.
    /// </summary>
    public EventSequenceId EventSequenceId { get; } = eventSequence ?? EventSequenceId.Log;

    /// <summary>
    /// Gets whether the webhook should be actively running on the Kernel.
    /// </summary>
    public bool IsActive { get; } = isActive;

    /// <summary>
    /// Gets whether the webhook is replayable or not. If it's not replayable, it will not be replayed when the definition changes.
    /// </summary>
    public bool IsReplayable { get; } = isActive;
}
