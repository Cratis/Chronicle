// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.EventSequences;

namespace Cratis.Chronicle.Webhooks;

/// <summary>
/// Defines a builder for <see cref="WebhookDefinition"/>.
/// </summary>
public interface IWebhookDefinitionBuilder
{
    /// <summary>
    /// Sets the <see cref="EventSequenceId"/> that the webhook observer should be registered on.
    /// </summary>
    /// <param name="eventSequenceId">The <see cref="EventSequenceId"/>.</param>
    /// <returns>The <see cref="IWebhookDefinitionBuilder"/> builder for continuation.</returns>
    IWebhookDefinitionBuilder OnEventSequence(EventSequenceId eventSequenceId);

    /// <summary>
    /// Use basic authentication.
    /// </summary>
    /// <param name="username">The username.</param>
    /// <param name="password">The password.</param>
    /// <returns>The <see cref="IWebhookDefinitionBuilder"/> builder for continuation.</returns>
    IWebhookDefinitionBuilder WithBasicAuth(string username, string password);

    /// <summary>
    /// Use bearer token authentication.
    /// </summary>
    /// <param name="token">The bearer token.</param>
    /// <returns>The <see cref="IWebhookDefinitionBuilder"/> builder for continuation.</returns>
    IWebhookDefinitionBuilder WithBearerToken(string token);

    /// <summary>
    /// Adds a header to the webhook requests.
    /// </summary>
    /// <param name="key">The header key.</param>
    /// <param name="value">The header value.</param>
    /// <returns>The <see cref="IWebhookDefinitionBuilder"/> builder for continuation.</returns>
    IWebhookDefinitionBuilder WithHeader(string key, string value);

    /// <summary>
    /// Adds an event type to the webhook observer.
    /// </summary>
    /// <typeparam name="TEventType">The <see cref="Type"/> of the event type.</typeparam>
    /// <returns>The <see cref="IWebhookDefinitionBuilder"/> builder for continuation.</returns>
    IWebhookDefinitionBuilder WithEventType<TEventType>();

    /// <summary>
    /// Specifies that the webhook is not replayable.
    /// </summary>
    /// <returns>The <see cref="IWebhookDefinitionBuilder"/> builder for continuation.</returns>
    IWebhookDefinitionBuilder NotReplayable();

    /// <summary>
    /// Specifies that the webhook is not active.
    /// </summary>
    /// <returns>The <see cref="IWebhookDefinitionBuilder"/> builder for continuation.</returns>
    IWebhookDefinitionBuilder NotActive();
}
