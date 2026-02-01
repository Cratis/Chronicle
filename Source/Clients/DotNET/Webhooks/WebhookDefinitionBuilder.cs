// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.Security;

namespace Cratis.Chronicle.Webhooks;

/// <summary>
/// Represents an implementation of <see cref="IWebhookDefinitionBuilder"/>.
/// </summary>
/// <param name="eventTypes">The <see cref="IEventTypes"/>.</param>
public class WebhookDefinitionBuilder(IEventTypes eventTypes) : IWebhookDefinitionBuilder
{
    readonly HashSet<EventType> _eventTypes = new();
    readonly Dictionary<string, string> _headers = new();
    EventSequenceId _eventSequenceId = EventSequenceId.Log;
    OneOf.OneOf<BasicAuthorization, BearerTokenAuthorization, OAuthAuthorization, OneOf.Types.None> _authorization = default(OneOf.Types.None);
    bool _isReplayable = true;
    bool _isActive = true;

    /// <inheritdoc/>
    public IWebhookDefinitionBuilder OnEventSequence(EventSequenceId eventSequenceId)
    {
        _eventSequenceId = eventSequenceId;
        return this;
    }

    /// <inheritdoc/>
    public IWebhookDefinitionBuilder WithBasicAuth(string username, string password)
    {
        _authorization = new BasicAuthorization(username, password);
        return this;
    }

    /// <inheritdoc/>
    public IWebhookDefinitionBuilder WithBearerToken(string token)
    {
        _authorization = new BearerTokenAuthorization(token);
        return this;
    }

    /// <inheritdoc/>
    public IWebhookDefinitionBuilder WithHeader(string key, string value)
    {
        _headers[key] = value;
        return this;
    }

    /// <inheritdoc/>
    public IWebhookDefinitionBuilder WithEventType<TEventType>()
    {
        var eventType = eventTypes.GetEventTypeFor(typeof(TEventType));
        _eventTypes.Add(eventType);
        return this;
    }

    /// <inheritdoc/>
    public IWebhookDefinitionBuilder NotReplayable()
    {
        _isReplayable = false;
        return this;
    }

    /// <inheritdoc/>
    public IWebhookDefinitionBuilder NotActive()
    {
        _isActive = false;
        return this;
    }

    /// <summary>
    /// Builds the <see cref="WebhookDefinition"/>.
    /// </summary>
    /// <param name="id">The <see cref="WebhookId"/>.</param>
    /// <param name="targetUrl">The <see cref="WebhookTargetUrl"/>.</param>
    /// <returns>The built <see cref="WebhookDefinition"/>.</returns>
    public WebhookDefinition Build(WebhookId id, WebhookTargetUrl targetUrl)
    {
        var target = new WebhookTarget(
            Url: targetUrl,
            Authorization: _authorization,
            Headers: _headers);

        return new WebhookDefinition(
            Identifier: id,
            EventTypes: _eventTypes.Count > 0 ? _eventTypes : eventTypes.All,
            Target: target,
            EventSequenceId: _eventSequenceId,
            IsReplayable: _isReplayable,
            IsActive: _isActive);
    }
}
