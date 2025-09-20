// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Observation.Webhooks;
using Orleans.Services;

namespace Cratis.Chronicle.Grains.Observation.Webhooks;

/// <summary>
/// Defines a service that is responsible for webhook registration on the silo.
/// </summary>
public interface IWebhooksService : IGrainService
{
    /// <summary>
    /// Register a set of <see cref="WebhookDefinition"/> for a specific event store.
    /// </summary>
    /// <param name="eventStore">Name of the event store.</param>
    /// <param name="definitions">A collection of <see cref="WebhookDefinition"/>.</param>
    /// <returns>Awaitable task.</returns>
    Task Register(EventStoreName eventStore, IEnumerable<WebhookDefinition> definitions);

    /// <summary>
    /// Triggered when a namespace is added to an event store.
    /// </summary>
    /// <param name="eventStore">Name of the event store.</param>
    /// <param name="namespace">Name of the namespace.</param>
    /// <returns>Awaitable task.</returns>
    Task NamespaceAdded(EventStoreName eventStore, EventStoreNamespaceName @namespace);
}
