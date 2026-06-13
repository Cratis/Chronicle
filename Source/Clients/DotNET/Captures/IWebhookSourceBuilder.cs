// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Captures;

/// <summary>
/// Defines the builder for configuring webhook capture sources.
/// </summary>
public interface IWebhookSourceBuilder
{
    /// <summary>
    /// Sets the authentication configuration.
    /// </summary>
    /// <param name="auth">The authentication configuration.</param>
    /// <returns>The builder continuation.</returns>
    IWebhookSourceBuilder WithAuth(string auth);
}
