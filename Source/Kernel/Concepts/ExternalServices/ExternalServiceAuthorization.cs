// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Serialization;
using Cratis.Chronicle.Concepts.Security;

namespace Cratis.Chronicle.Concepts.ExternalServices;

/// <summary>
/// Represents the authorization used when connecting to an HTTP external service.
/// </summary>
[JsonConverter(typeof(ExternalServiceAuthorizationJsonConverter))]
public sealed class ExternalServiceAuthorization : OneOf.OneOfBase<BasicAuthorization, BearerTokenAuthorization, OAuthAuthorization, OneOf.Types.None>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ExternalServiceAuthorization"/> class.
    /// </summary>
    /// <param name="input">The input value.</param>
    ExternalServiceAuthorization(OneOf.OneOf<BasicAuthorization, BearerTokenAuthorization, OAuthAuthorization, OneOf.Types.None> input) : base(input)
    {
    }

    /// <summary>
    /// Gets an <see cref="ExternalServiceAuthorization"/> representing no authorization.
    /// </summary>
    public static ExternalServiceAuthorization None => new(OneOf.OneOf<BasicAuthorization, BearerTokenAuthorization, OAuthAuthorization, OneOf.Types.None>.FromT3(default));

    /// <summary>
    /// Implicitly converts from <see cref="BasicAuthorization"/> to <see cref="ExternalServiceAuthorization"/>.
    /// </summary>
    /// <param name="value">The value to convert.</param>
    public static implicit operator ExternalServiceAuthorization(BasicAuthorization value) => new(value);

    /// <summary>
    /// Implicitly converts from <see cref="BearerTokenAuthorization"/> to <see cref="ExternalServiceAuthorization"/>.
    /// </summary>
    /// <param name="value">The value to convert.</param>
    public static implicit operator ExternalServiceAuthorization(BearerTokenAuthorization value) => new(value);

    /// <summary>
    /// Implicitly converts from <see cref="OAuthAuthorization"/> to <see cref="ExternalServiceAuthorization"/>.
    /// </summary>
    /// <param name="value">The value to convert.</param>
    public static implicit operator ExternalServiceAuthorization(OAuthAuthorization value) => new(value);

    /// <summary>
    /// Implicitly converts from <see cref="OneOf.Types.None"/> to <see cref="ExternalServiceAuthorization"/>.
    /// </summary>
    /// <param name="value">The value to convert.</param>
#pragma warning disable IDE0060 // Remove unused parameter
    public static implicit operator ExternalServiceAuthorization(OneOf.Types.None value) => None;
#pragma warning restore IDE0060 // Remove unused parameter
}
