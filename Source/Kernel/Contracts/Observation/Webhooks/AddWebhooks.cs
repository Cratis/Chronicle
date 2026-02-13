// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Contracts.Observation.Webhooks;

/// <summary>
/// Represents the payload for registering an observer.
/// </summary>
[ProtoContract]
public class AddWebhooks
{
    /// <summary>
    /// Gets or sets the event store name.
    /// </summary>
    [ProtoMember(1)]
    public string EventStore { get; set; }

    /// <summary>
    /// Gets or sets the <see cref="ObserverOwner"/>.
    /// </summary>
    [ProtoMember(2)]
    public ObserverOwner Owner { get; set; }

    /// <summary>
    /// Gets or sets the collection of  <see cref="WebhookDefinition"/> instances to register.
    /// </summary>
    [ProtoMember(3, IsRequired = true)]
    public IList<WebhookDefinition> Webhooks { get; set; } = [];
}
