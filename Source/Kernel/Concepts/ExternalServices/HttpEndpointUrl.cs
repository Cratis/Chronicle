// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Concepts.ExternalServices;

/// <summary>
/// Represents the base URL of an HTTP external service endpoint.
/// </summary>
/// <param name="Value">The actual value.</param>
public record HttpEndpointUrl(string Value) : ConceptAs<string>(Value)
{
    /// <summary>
    /// Implicitly convert from a string to <see cref="HttpEndpointUrl"/>.
    /// </summary>
    /// <param name="url">String to convert from.</param>
    public static implicit operator HttpEndpointUrl(string url) => new(url);
}
