// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.Observation;

namespace Cratis.Chronicle.Webhooks;

public interface IWebhookDefinitionBuilder
{
    IWebhookDefinitionBuilder OnEventSequence(EventSequenceId eventSequenceId);
    IWebhookDefinitionBuilder WithBasicAuth(string username, string password);
    IWebhookDefinitionBuilder WithBearerToken(string token);
    IWebhookDefinitionBuilder WithHeader(string key, string value);
    IWebhookDefinitionBuilder WithEventType<TEventType>();

    WebhookDefinition Build();
}

public class WebhookDefinitionBuilder(IEventTypes eventTypes) : IWebhookDefinitionBuilder
{
    public IWebhookDefinitionBuilder OnEventSequence(EventSequenceId eventSequenceId) => throw new NotImplementedException();
    public IWebhookDefinitionBuilder WithBasicAuth(string username, string password) => throw new NotImplementedException();
    public IWebhookDefinitionBuilder WithBearerToken(string token) => throw new NotImplementedException();
    public IWebhookDefinitionBuilder WithHeader(string key, string value) => throw new NotImplementedException();
    public IWebhookDefinitionBuilder WithEventType<TEventType>() => throw new NotImplementedException();
    public WebhookDefinition Build() => throw new NotImplementedException();
}

/// <summary>
/// Defines a system for working with webhook registrations for the Kernel.
/// </summary>
public interface IWebhooks
{
    /// <summary>
    /// Registers a webhook.
    /// </summary>
    /// <param name="webhookId">The <see cref="WebhookId"/> of the webhook to register.</param>
    /// <returns>Awaitable task.</returns>
    Task Register(WebhookId webhookId, WebhookTargetUrl targetUrl);

    /// <summary>
    /// Registers a webhook.
    /// </summary>
    /// <param name="webhook">The <see cref="WebhookDefinition"/> to register.</param>
    /// <returns>Awaitable task.</returns>
    Task Register(WebhookDefinition webhook);

    /// <summary>
    /// Get any failed partitions for a specific webhook.
    /// </summary>
    /// <param name="webhookId">The <see cref="WebhookId"/>.</param>
    /// <returns>Collection of <see cref="FailedPartition"/>, if any.</returns>
    Task<IEnumerable<FailedPartition>> GetFailedPartitionsFor(WebhookId webhookId);

    /// <summary>
    /// Get the state of a specific webhook observer.
    /// </summary>
    /// <param name="webhookId">The <see cref="WebhookId"/>.</param>
    /// <returns><see cref="webhookState"/>.</returns>
    Task<WebhookState> GetStateFor(WebhookId webhookId);

    /// <summary>
    /// Replay a specific webhook by its identifier.
    /// </summary>
    /// <param name="webhookId"><see cref="WebhookId"/> to replay.</param>
    /// <returns>Awaitable task.</returns>
    Task Replay(WebhookId webhookId);
}
