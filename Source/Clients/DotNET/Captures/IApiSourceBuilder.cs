// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Captures;

/// <summary>
/// Defines the builder for configuring API capture sources.
/// </summary>
public interface IApiSourceBuilder
{
    /// <summary>
    /// Sets how often the API source should be polled.
    /// </summary>
    /// <param name="interval">The poll interval.</param>
    /// <returns>The builder continuation.</returns>
    IApiSourceBuilder PollEvery(string interval);

    /// <summary>
    /// Sets the route to poll on the configured API source.
    /// </summary>
    /// <param name="route">The route to use.</param>
    /// <returns>The builder continuation.</returns>
    IApiSourceBuilder OnRoute(string route);

    /// <summary>
    /// Sets a bearer token authentication configuration.
    /// </summary>
    /// <param name="token">The bearer token expression.</param>
    /// <returns>The builder continuation.</returns>
    IApiSourceBuilder WithBearerToken(string token);

    /// <summary>
    /// Sets the raw authentication configuration.
    /// </summary>
    /// <param name="auth">The authentication configuration.</param>
    /// <returns>The builder continuation.</returns>
    IApiSourceBuilder WithAuth(string auth);
}
