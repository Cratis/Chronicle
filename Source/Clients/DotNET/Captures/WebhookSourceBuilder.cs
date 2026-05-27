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
    string? _auth;

    /// <inheritdoc/>
    public IWebhookSourceBuilder WithAuth(string auth)
    {
        _auth = auth;

        return this;
    }

    /// <summary>
    /// Builds the <see cref="SourceDefinition"/>.
    /// </summary>
    /// <returns>A new <see cref="SourceDefinition"/>.</returns>
    public SourceDefinition Build() => new(SourceType.Webhook, Auth: _auth, Path: path);
}
