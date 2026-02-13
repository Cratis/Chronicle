// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reactive.Subjects;
using Cratis.Chronicle.Concepts.Observation.Webhooks;

namespace Cratis.Chronicle.Storage.Observation.Webhooks;

/// <summary>
/// Defines a system for working with <see cref="WebhookDefinition"/>.
/// </summary>
public interface IWebhookDefinitionsStorage
{
    /// <summary>
    /// Get all <see cref="WebhookDefinition">definitions</see> registered.
    /// </summary>
    /// <returns>A collection of <see cref="WebhookDefinition"/>.</returns>
    Task<IEnumerable<WebhookDefinition>> GetAll();

    /// <summary>
    /// Get all <see cref="WebhookDefinition">definitions</see> registered.
    /// </summary>
    /// <returns>A <see cref="ISubject{T}"/> of <see cref="IEnumerable{T}"/> of <see cref="WebhookDefinition"/>.</returns>
    ISubject<IEnumerable<WebhookDefinition>> ObserveAll();

    /// <summary>
    /// Check if a <see cref="WebhookDefinition"/> exists by its <see cref="WebhookId"/>.
    /// </summary>
    /// <param name="id"><see cref="WebhookId"/> to check for.</param>
    /// <returns>True if it exists, false if not.</returns>
    Task<bool> Has(WebhookId id);

    /// <summary>
    /// Get a specific <see cref="WebhookDefinition"/> by its <see cref="WebhookId"/>.
    /// </summary>
    /// <param name="id"><see cref="WebhookId"/> to get for.</param>
    /// <returns><see cref="WebhookDefinition"/>.</returns>
    Task<WebhookDefinition> Get(WebhookId id);

    /// <summary>
    /// Delete a <see cref="WebhookDefinition"/> by its <see cref="WebhookId"/>.
    /// </summary>
    /// <param name="id"><see cref="WebhookId"/> of the <see cref="WebhookDefinition"/> to delete.</param>
    /// <returns>Awaitable task.</returns>
    Task Delete(WebhookId id);

    /// <summary>
    /// Save a <see cref="WebhookDefinition"/>.
    /// </summary>
    /// <param name="definition">Definition to save.</param>
    /// <returns>Async task.</returns>
    Task Save(WebhookDefinition definition);
}
