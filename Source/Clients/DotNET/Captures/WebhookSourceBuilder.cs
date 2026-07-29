// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Captures;

namespace Cratis.Chronicle.Captures;

/// <summary>
/// Represents an implementation of <see cref="IWebhookSourceBuilder"/>.
/// </summary>
/// <param name="path">The webhook path.</param>
public class WebhookSourceBuilder(string path) : IWebhookSourceBuilder
{
    SourceAuthorization? _authorization;

    /// <inheritdoc/>
    public IWebhookSourceBuilder WithBasicAuth(string username, string password)
    {
        _authorization = new SourceBasicAuthorization(username, password);

        return this;
    }

    /// <inheritdoc/>
    public IWebhookSourceBuilder WithBearerToken(string token)
    {
        _authorization = new SourceBearerTokenAuthorization(token);

        return this;
    }

    /// <inheritdoc/>
    public IWebhookSourceBuilder WithOAuth(string authority, string clientId, string clientSecret)
    {
        _authorization = new SourceOAuthAuthorization(authority, clientId, clientSecret);

        return this;
    }

    /// <summary>
    /// Builds the <see cref="SourceDefinition"/>.
    /// </summary>
    /// <returns>A new <see cref="SourceDefinition"/>.</returns>
    public SourceDefinition Build() => new(SourceType.Webhook, Path: path, Authorization: _authorization);
}
