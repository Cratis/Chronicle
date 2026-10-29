// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Net;
using Cratis.Chronicle.Concepts;

namespace Cratis.Chronicle.Observation.Webhooks.Validation;

/// <summary>
/// Evaluates webhook and OAuth destinations, including stored targets before delivery.
/// </summary>
public interface IWebhookTargetPolicy
{
    /// <summary>
    /// Checks operator approval of the managed identity, scope and receiver host in every validation mode.
    /// </summary>
    /// <param name="authorization">The requested identity and scope.</param>
    /// <param name="url">The receiver URL.</param>
    /// <param name="eventStore">The event store, or null for global host policy.</param>
    /// <returns>The approval decision and a safe reason.</returns>
    WebhookTargetValidation CheckManagedIdentity(Concepts.Security.ManagedIdentityAuthorization authorization, string url, EventStoreName? eventStore = null);

    /// <summary>
    /// Evaluates a target and its current DNS answers without contacting the receiver.
    /// </summary>
    /// <param name="url">The target URL.</param>
    /// <param name="eventStore">The event store, or null for global policy.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The policy decision and a safe reason.</returns>
    Task<WebhookTargetValidation> Evaluate(string url, EventStoreName? eventStore = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Evaluates a delivery destination without repeating activation warnings.
    /// </summary>
    /// <param name="url">The target URL.</param>
    /// <param name="eventStore">The event store, or null for global policy.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The policy decision and a safe reason.</returns>
    Task<WebhookTargetValidation> EvaluateForDelivery(string url, EventStoreName? eventStore = null, CancellationToken cancellationToken = default) =>
        Evaluate(url, eventStore, cancellationToken);

    /// <summary>
    /// Evaluates scheme and host on every request, including pooled connections.
    /// </summary>
    /// <param name="target">The target URI.</param>
    /// <param name="eventStore">The event store, or null for global policy.</param>
    /// <returns>The policy decision.</returns>
    WebhookTargetValidation CheckAuthority(Uri target, EventStoreName? eventStore);

    /// <summary>
    /// Determines whether an explicit host allowance requires a dedicated connection.
    /// </summary>
    /// <param name="target">The target URI.</param>
    /// <param name="eventStore">The event store, or null for global host policy.</param>
    /// <returns>Whether the request must not lend its connection to another request.</returns>
    bool RequiresDedicatedConnection(Uri target, EventStoreName? eventStore);

    /// <summary>
    /// Evaluates all resolved addresses before any socket is opened.
    /// </summary>
    /// <param name="target">The target URI.</param>
    /// <param name="addresses">The resolved addresses.</param>
    /// <param name="eventStore">The event store, or null for global policy.</param>
    /// <returns>The policy decision.</returns>
    WebhookTargetValidation CheckAddresses(Uri target, IEnumerable<IPAddress> addresses, EventStoreName? eventStore);
}
