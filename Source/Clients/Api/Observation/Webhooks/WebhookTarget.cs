// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Api.Observation.Webhooks;

/// <summary>
/// Represents the target of a webhook.
/// </summary>
/// <param name="Url">The target url.</param>
/// <param name="Headers">The headers.</param>
public record WebhookTarget(
    string Url,
#pragma warning disable MA0016
    Dictionary<string, string> Headers)
#pragma warning restore MA0016
{
    /// <summary>
    /// The optional <see cref="BasicAuthorization"/>.
    /// </summary>
    public BasicAuthorization? BasicAuthorization { get; init; }

    /// <summary>
    /// The optional <see cref="BasicAuthorization"/>.
    /// </summary>
    public BearerTokenAuthorization? BearerTokenAuthorization { get; init; }

    /// <summary>
    /// The optional <see cref="BasicAuthorization"/>.
    /// </summary>
    public OAuthAuthorization? OAuthAuthorization { get; init; }

    /// <summary>
    /// Creates <see cref="WebhookTarget" /> with the given authorization type.
    /// </summary>
    /// <param name="authorization">The authorization.</param>
    /// <returns>The new <see cref="WebhookTarget"/>.</returns>
    public WebhookTarget WithAuth(OneOf.OneOf<BasicAuthorization, BearerTokenAuthorization, OAuthAuthorization, OneOf.Types.None> authorization)
    {
        return authorization.Match(
            basic => this with { BasicAuthorization = basic },
            bearer => this with { BearerTokenAuthorization = bearer },
            oauth => this with { OAuthAuthorization = oauth },
            _ => this);
    }
}
