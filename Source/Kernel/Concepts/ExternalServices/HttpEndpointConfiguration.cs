// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Concepts.ExternalServices;

/// <summary>
/// Represents the configuration for an HTTP external service endpoint.
/// </summary>
/// <param name="Url">The base URL of the endpoint.</param>
/// <param name="Authorization">The authorization used when connecting to the endpoint.</param>
/// <param name="Headers">Additional headers to send with every request.</param>
public record HttpEndpointConfiguration(
    HttpEndpointUrl Url,
    ExternalServiceAuthorization Authorization,
    IReadOnlyDictionary<string, string> Headers);
