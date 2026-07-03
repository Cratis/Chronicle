// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Captures;

/// <summary>
/// Defines the builder for configuring API capture sources.
/// </summary>
/// <remarks>
/// The API source references a configured External Service by name. The base URL and authentication
/// for the connection are configured on the External Service, not on the capture - so no authentication
/// is configured here.
/// </remarks>
public interface IApiSourceBuilder
{
    /// <summary>
    /// Sets how often the API source should be polled.
    /// </summary>
    /// <param name="interval">The poll interval.</param>
    /// <returns>The builder continuation.</returns>
    IApiSourceBuilder PollEvery(string interval);

    /// <summary>
    /// Sets the route to poll on the referenced External Service, relative to its configured base URL.
    /// </summary>
    /// <param name="route">The route to use.</param>
    /// <returns>The builder continuation.</returns>
    IApiSourceBuilder OnRoute(string route);
}
