// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Contracts.Observation.Webhooks;

/// <summary>
/// Represents the payload for unregistering a webhook.
/// </summary>
[ProtoContract]
public class UnregisterWebhook
{
    /// <summary>
    /// Gets or sets the event store name.
    /// </summary>
    [ProtoMember(1)]
    public string EventStore { get; set; }

    /// <summary>
    /// Gets or sets the collection of webhook ids to unregister.
    /// </summary>
    [ProtoMember(3, IsRequired = true)]
    public IList<string> Webhooks { get; set; } = [];
}