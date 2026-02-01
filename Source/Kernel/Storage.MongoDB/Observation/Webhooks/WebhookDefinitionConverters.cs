// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#pragma warning disable IDE0005 // Using directive is unnecessary

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Observation.Webhooks;
using Cratis.Chronicle.Concepts.Security;
using Cratis.Chronicle.Storage.MongoDB.Security;

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
        WebhookAuthorization authorization;

        if (target.BasicAuthorization is not null)
        {
            authorization = new Concepts.Security.BasicAuthorization(target.BasicAuthorization.Username, target.BasicAuthorization.Password);
        }
        else if (target.BearerTokenAuthorization is not null)
        {
            authorization = new Concepts.Security.BearerTokenAuthorization(target.BearerTokenAuthorization.Token);
        }
        else if (target.OAuthAuthorization is not null)
        {
            authorization = new Concepts.Security.OAuthAuthorization(
                target.OAuthAuthorization.Authority,
                target.OAuthAuthorization.ClientId,
                target.OAuthAuthorization.ClientSecret);
        }
        else
        {
            authorization = WebhookAuthorization.None;
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
            basic => mongoTarget.BasicAuthorization = new MongoDB.Security.BasicAuthorization
            {
                Username = basic.Username.Value,
                Password = basic.Password.Value
            },
            bearer => mongoTarget.BearerTokenAuthorization = new MongoDB.Security.BearerTokenAuthorization
            {
                Token = bearer.Token.Value
            },
            oauth => mongoTarget.OAuthAuthorization = new MongoDB.Security.OAuthAuthorization
            {
                Authority = oauth.Authority.Value,
                ClientId = oauth.ClientId.Value,
                ClientSecret = oauth.ClientSecret.Value
            },
            none => { });

        return mongoTarget;
    }
}
