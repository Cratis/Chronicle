// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#pragma warning disable IDE0005 // Using directive is unnecessary

using Cratis.Chronicle.Concepts.Events;
using OneOf.Types;

namespace Cratis.Chronicle.Storage.MongoDB.Observation.Webhooks;

/// <summary>
/// Provides extension methods for converting between Kernel and MongoDB <see cref="WebhookDefinition"/> representations.
/// </summary>
public static class WebhookDefinitionConverters
{
    /// <summary>
    /// Converts a Kernel <see cref="Concepts.Observation.Reactors.ReactorDefinition"/> to a MongoDB <see cref="WebhookDefinition"/>.
    /// </summary>
    /// <param name="definition">The Kernel reactor definition.</param>
    /// <returns>The MongoDB reactor definition.</returns>
    public static WebhookDefinition ToMongoDB(this Concepts.Observation.Webhooks.WebhookDefinition definition) =>
        new()
        {
            Id = definition.Identifier,
            Owner = definition.Owner,
            EventSequenceId = definition.EventSequenceId,
            EventTypes = definition.EventTypes.ToDictionary(
                et => et.ToString(),
                et => "$eventSourceId"),
            Target = definition.Target.ToMongoDB(),
            IsReplayable = definition.IsReplayable,
            IsActive = definition.IsActive
        };

    /// <summary>
    /// Converts a MongoDB <see cref="WebhookDefinition"/> to a Kernel <see cref="Concepts.Observation.Reactors.ReactorDefinition"/>.
    /// </summary>
    /// <param name="definition">The MongoDB reactor definition.</param>
    /// <returns>The Kernel reactor definition.</returns>
    public static Concepts.Observation.Webhooks.WebhookDefinition ToKernel(this WebhookDefinition definition) =>
        new(
            definition.Id,
            definition.Owner,
            definition.EventSequenceId,
            definition.EventTypes.Select(kvp => EventType.Parse(kvp.Key)),
            definition.Target.ToKernel(),
            definition.IsReplayable);

    static Concepts.Observation.Webhooks.WebhookTarget ToKernel(this WebhookTarget target)
    {
        OneOf.OneOf<Concepts.Observation.Webhooks.BasicAuthorization, Concepts.Observation.Webhooks.BearerTokenAuthorization, Concepts.Observation.Webhooks.OAuthAuthorization, None> authorization;

        if (target.BasicAuthorizationUsername is not null && target.BasicAuthorizationPassword is not null)
        {
            authorization = new Concepts.Observation.Webhooks.BasicAuthorization(target.BasicAuthorizationUsername, target.BasicAuthorizationPassword);
        }
        else if (target.BearerToken is not null)
        {
            authorization = new Concepts.Observation.Webhooks.BearerTokenAuthorization(target.BearerToken);
        }
        else if (target.OAuthAuthority is not null && target.OAuthClientId is not null && target.OAuthClientSecret is not null)
        {
            authorization = new Concepts.Observation.Webhooks.OAuthAuthorization(
                target.OAuthAuthority,
                target.OAuthClientId,
                target.OAuthClientSecret);
        }
        else
        {
            authorization = default(None);
        }

        return new(
            target.Url,
            authorization,
            target.Headers.AsReadOnly());
    }

    static WebhookTarget ToMongoDB(this Concepts.Observation.Webhooks.WebhookTarget target)
    {
        var mongoTarget = new WebhookTarget
        {
            Url = target.Url,
            Headers = target.Headers.ToDictionary(kvp => kvp.Key, kvp => kvp.Value)
        };

        target.Authorization.Switch(
            basic =>
            {
                mongoTarget.BasicAuthorizationUsername = basic.Username;
                mongoTarget.BasicAuthorizationPassword = basic.Password;
            },
            bearer => mongoTarget.BearerToken = bearer.Token,
            oauth =>
            {
                mongoTarget.OAuthAuthority = oauth.Authority;
                mongoTarget.OAuthClientId = oauth.ClientId;
                mongoTarget.OAuthClientSecret = oauth.ClientSecret;
            },
            none => { });

        return mongoTarget;
    }
}
