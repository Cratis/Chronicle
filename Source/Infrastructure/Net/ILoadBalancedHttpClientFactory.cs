// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Net;

/// <summary>
/// Defines a load balanced factory for <see cref="HttpClient"/>.
/// </summary>
public interface ILoadBalancedHttpClientFactory
{
    /// <summary>
    /// Create a <see cref="HttpClient"/> for the next endpoint.
    /// </summary>
    /// <param name="endpoints">A collection of endpoints it selects from.</param>
    /// <param name="name">Optional name associated with the client.</param>
    /// <returns>A <see cref="HttpClient"/> for the endpoint.</returns>
    /// <remarks>The `BaseAddress` of the client gets the endpoint set.</remarks>
    HttpClient Create(IEnumerable<Uri> endpoints, string? name = default);
}
