// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Concepts.Security;

/// <summary>
/// Represents an API key for authentication.
/// </summary>
/// <param name="Value">The API key value.</param>
public record ApiKey(string Value) : ConceptAs<string>(Value)
{
    /// <summary>
    /// Gets an <see cref="ApiKey"/> representing an empty or not set API key.
    /// </summary>
    public static readonly ApiKey NotSet = new(string.Empty);

    /// <summary>
    /// Implicitly converts from <see cref="string"/> to <see cref="ApiKey"/>.
    /// </summary>
    /// <param name="value">The value to convert.</param>
    public static implicit operator ApiKey(string value) => new(value);

    /// <summary>
    /// Implicitly converts from <see cref="ApiKey"/> to <see cref="string"/>.
    /// </summary>
    /// <param name="apiKey">The API key to convert.</param>
    public static implicit operator string(ApiKey apiKey) => apiKey.Value;
}
