// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Serialization;
using Cratis.Chronicle.Concepts.Security;

namespace Cratis.Chronicle.Concepts.Observation.Webhooks;

/// <summary>
/// Represents the authorization for a webhook.
/// </summary>
[JsonConverter(typeof(WebhookAuthorizationJsonConverter))]
public sealed class WebhookAuthorization : OneOf.OneOfBase<BasicAuthorization, BearerTokenAuthorization, OAuthAuthorization, OneOf.Types.None>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="WebhookAuthorization"/> class.
    /// </summary>
    /// <param name="input">The input value.</param>
    WebhookAuthorization(OneOf.OneOf<BasicAuthorization, BearerTokenAuthorization, OAuthAuthorization, OneOf.Types.None> input) : base(input)
    {
    }

    /// <summary>
    /// Gets a <see cref="WebhookAuthorization"/> representing no authorization.
    /// </summary>
    public static WebhookAuthorization None => new(OneOf.OneOf<BasicAuthorization, BearerTokenAuthorization, OAuthAuthorization, OneOf.Types.None>.FromT3(default));

    /// <summary>
    /// Implicitly converts from <see cref="BasicAuthorization"/> to <see cref="WebhookAuthorization"/>.
    /// </summary>
    /// <param name="value">The value to convert.</param>
    public static implicit operator WebhookAuthorization(BasicAuthorization value) => new(value);

    /// <summary>
    /// Implicitly converts from <see cref="BearerTokenAuthorization"/> to <see cref="WebhookAuthorization"/>.
    /// </summary>
    /// <param name="value">The value to convert.</param>
    public static implicit operator WebhookAuthorization(BearerTokenAuthorization value) => new(value);

    /// <summary>
    /// Implicitly converts from <see cref="OAuthAuthorization"/> to <see cref="WebhookAuthorization"/>.
    /// </summary>
    /// <param name="value">The value to convert.</param>
    public static implicit operator WebhookAuthorization(OAuthAuthorization value) => new(value);

    /// <summary>
    /// Implicitly converts from <see cref="OneOf.Types.None"/> to <see cref="WebhookAuthorization"/>.
    /// </summary>
    /// <param name="value">The value to convert.</param>
#pragma warning disable IDE0060 // Remove unused parameter
    public static implicit operator WebhookAuthorization(OneOf.Types.None value) => None;
#pragma warning restore IDE0060 // Remove unused parameter
}
