// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Serialization;

namespace Cratis.Chronicle.Concepts.Captures;

/// <summary>
/// Represents the authentication configuration for a capture source.
/// </summary>
/// <remarks>
/// Authentication is configured in code when the source is configured - it is intentionally not
/// part of the Capture Declaration Language, so that secrets and tokens never live in capture text.
/// </remarks>
[JsonConverter(typeof(SourceAuthorizationJsonConverter))]
public sealed class SourceAuthorization : OneOf.OneOfBase<SourceBasicAuthorization, SourceBearerTokenAuthorization, SourceOAuthAuthorization, OneOf.Types.None>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="SourceAuthorization"/> class.
    /// </summary>
    /// <param name="input">The input value.</param>
    SourceAuthorization(OneOf.OneOf<SourceBasicAuthorization, SourceBearerTokenAuthorization, SourceOAuthAuthorization, OneOf.Types.None> input) : base(input)
    {
    }

    /// <summary>
    /// Gets a <see cref="SourceAuthorization"/> representing no authorization.
    /// </summary>
    public static SourceAuthorization None => new(OneOf.OneOf<SourceBasicAuthorization, SourceBearerTokenAuthorization, SourceOAuthAuthorization, OneOf.Types.None>.FromT3(default));

    /// <summary>
    /// Implicitly converts from <see cref="SourceBasicAuthorization"/> to <see cref="SourceAuthorization"/>.
    /// </summary>
    /// <param name="value">The value to convert.</param>
    public static implicit operator SourceAuthorization(SourceBasicAuthorization value) => new(value);

    /// <summary>
    /// Implicitly converts from <see cref="SourceBearerTokenAuthorization"/> to <see cref="SourceAuthorization"/>.
    /// </summary>
    /// <param name="value">The value to convert.</param>
    public static implicit operator SourceAuthorization(SourceBearerTokenAuthorization value) => new(value);

    /// <summary>
    /// Implicitly converts from <see cref="SourceOAuthAuthorization"/> to <see cref="SourceAuthorization"/>.
    /// </summary>
    /// <param name="value">The value to convert.</param>
    public static implicit operator SourceAuthorization(SourceOAuthAuthorization value) => new(value);

    /// <summary>
    /// Implicitly converts from <see cref="OneOf.Types.None"/> to <see cref="SourceAuthorization"/>.
    /// </summary>
    /// <param name="value">The value to convert.</param>
#pragma warning disable IDE0060 // Remove unused parameter
    public static implicit operator SourceAuthorization(OneOf.Types.None value) => None;
#pragma warning restore IDE0060 // Remove unused parameter
}
