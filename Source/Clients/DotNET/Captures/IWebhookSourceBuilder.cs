// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Captures;

/// <summary>
/// Defines the builder for configuring webhook capture sources.
/// </summary>
public interface IWebhookSourceBuilder
{
    /// <summary>
    /// Configures basic authentication for the source.
    /// </summary>
    /// <param name="username">The username.</param>
    /// <param name="password">The password.</param>
    /// <returns>The builder continuation.</returns>
    IWebhookSourceBuilder WithBasicAuth(string username, string password);

    /// <summary>
    /// Configures bearer token authentication for the source.
    /// </summary>
    /// <param name="token">The bearer token.</param>
    /// <returns>The builder continuation.</returns>
    IWebhookSourceBuilder WithBearerToken(string token);

    /// <summary>
    /// Configures OAuth authentication for the source.
    /// </summary>
    /// <param name="authority">The OAuth authority.</param>
    /// <param name="clientId">The OAuth client ID.</param>
    /// <param name="clientSecret">The OAuth client secret.</param>
    /// <returns>The builder continuation.</returns>
    IWebhookSourceBuilder WithOAuth(string authority, string clientId, string clientSecret);
}
