// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSequences;

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

#pragma warning disable RCS1181
    // AuthenticationType _authentication = AuthenticationType.None;
    // string? _username;
    // string? _password;
    // string? _bearerToken;
#pragma warning restore RCS1181
    bool _isReplayable = true;
    bool _isActive = true;

    /// <inheritdoc/>
    public IWebhookDefinitionBuilder OnEventSequence(EventSequenceId eventSequenceId)
    {
        _eventSequenceId = eventSequenceId;
        return this;
    }

    // /// <inheritdoc/>
    // public IWebhookDefinitionBuilder WithBasicAuth(string username, string password)
    // {
    //     _authentication = AuthenticationType.Basic;
    //     _username = username;
    //     _password = password;
    //     return this;
    // }
    //
    // /// <inheritdoc/>
    // public IWebhookDefinitionBuilder WithBearerToken(string token)
    // {
    //     _authentication = AuthenticationType.Bearer;
    //     _bearerToken = token;
    //     return this;
    // }

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
            Authentication: AuthenticationType.None,
            Username: null,
            Password: null,
            BearerToken: null,
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
