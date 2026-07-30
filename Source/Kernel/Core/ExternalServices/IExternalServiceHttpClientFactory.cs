// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.ExternalServices;

namespace Cratis.Chronicle.ExternalServices;

/// <summary>
/// Defines a factory for creating <see cref="HttpClient"/> instances configured for an external service's HTTP endpoint,
/// including its authorization.
/// </summary>
public interface IExternalServiceHttpClientFactory
{
    /// <summary>
    /// Create an <see cref="HttpClient"/> for the given external service.
    /// </summary>
    /// <param name="externalService">The <see cref="ExternalServiceDefinition"/> to create for.</param>
    /// <returns>A configured <see cref="HttpClient"/>.</returns>
    /// <remarks>
    /// OAuth authorized endpoints have a client credentials token acquired as part of creating the client.
    /// </remarks>
    Task<HttpClient> Create(ExternalServiceDefinition externalService);
}
