// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#pragma warning disable IDE0005 // Using directive is unnecessary

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Observation.Webhooks;
using Cratis.Chronicle.Concepts.Security;
using Cratis.Chronicle.Grains.Observation.Webhooks;
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
    /// <param name="encryption">The encryption service for secrets.</param>
    /// <returns>The MongoDB reactor definition.</returns>
    public static WebhookDefinition ToMongoDB(this Concepts.Observation.Webhooks.WebhookDefinition definition, IWebhookSecretEncryption encryption) =>
        new()
        {
            Id = definition.Identifier,
            Owner = definition.Owner,
            EventSequenceId = definition.EventSequenceId,
            EventTypes = definition.EventTypes.ToDictionary(
                et => et.ToString(),
                et => "$eventSourceId"),
            Target = definition.Target.ToMongoDB(encryption),
            IsReplayable = definition.IsReplayable,
            IsActive = definition.IsActive
        };

    /// <summary>
    /// Converts a MongoDB <see cref="WebhookDefinition"/> to a Kernel <see cref="Concepts.Observation.Reactors.ReactorDefinition"/>.
    /// </summary>
    /// <param name="definition">The MongoDB reactor definition.</param>
    /// <param name="encryption">The encryption service for secrets.</param>
    /// <returns>The Kernel reactor definition.</returns>
    public static Concepts.Observation.Webhooks.WebhookDefinition ToKernel(this WebhookDefinition definition, IWebhookSecretEncryption encryption) =>
        new(
            definition.Id,
            definition.Owner,
            definition.EventSequenceId,
            definition.EventTypes.Select(kvp => EventType.Parse(kvp.Key)),
            definition.Target.ToKernel(encryption),
            definition.IsReplayable);

    static Concepts.Observation.Webhooks.WebhookTarget ToKernel(this WebhookTarget target, IWebhookSecretEncryption encryption)
    {
        WebhookAuthorization authorization;

        if (target.BasicAuthorization is not null)
        {
            var decryptedPassword = encryption.Decrypt(target.BasicAuthorization.Password);
            authorization = new Concepts.Security.BasicAuthorization(target.BasicAuthorization.Username, decryptedPassword);
        }
        else if (target.BearerTokenAuthorization is not null)
        {
            var decryptedToken = encryption.Decrypt(target.BearerTokenAuthorization.Token);
            authorization = new Concepts.Security.BearerTokenAuthorization(decryptedToken);
        }
        else if (target.OAuthAuthorization is not null)
        {
            var decryptedClientSecret = encryption.Decrypt(target.OAuthAuthorization.ClientSecret);
            authorization = new Concepts.Security.OAuthAuthorization(
                target.OAuthAuthorization.Authority,
                target.OAuthAuthorization.ClientId,
                decryptedClientSecret);
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

    static WebhookTarget ToMongoDB(this Concepts.Observation.Webhooks.WebhookTarget target, IWebhookSecretEncryption encryption)
    {
        var mongoTarget = new WebhookTarget
        {
            Url = target.Url,
            Headers = target.Headers.ToDictionary(kvp => kvp.Key, kvp => kvp.Value)
        };

        target.Authorization.Switch(
            basic => mongoTarget.BasicAuthorization = new MongoDB.Security.BasicAuthorization
            {
                Username = basic.Username,
                Password = encryption.Encrypt(basic.Password)
            },
            bearer => mongoTarget.BearerTokenAuthorization = new MongoDB.Security.BearerTokenAuthorization
            {
                Token = encryption.Encrypt(bearer.Token)
            },
            oauth => mongoTarget.OAuthAuthorization = new MongoDB.Security.OAuthAuthorization
            {
                Authority = oauth.Authority,
                ClientId = oauth.ClientId,
                ClientSecret = encryption.Encrypt(oauth.ClientSecret)
            },
            none => { });

        return mongoTarget;
    }
}
