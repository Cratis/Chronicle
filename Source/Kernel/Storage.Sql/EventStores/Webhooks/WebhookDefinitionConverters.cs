// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Observation.Webhooks;
using Cratis.Chronicle.Concepts.Security;

namespace Cratis.Chronicle.Storage.Sql.EventStores.Webhooks;

/// <summary>
/// Provides extension methods for converting between Kernel and SQL <see cref="WebhookDefinition"/> representations.
/// </summary>
public static class WebhookDefinitionConverters
{
    /// <summary>
    /// Converts a Kernel <see cref="Concepts.Observation.Webhooks.WebhookDefinition"/> to a SQL <see cref="WebhookDefinition"/>.
    /// </summary>
    /// <param name="definition">The Kernel webhook definition.</param>
    /// <returns>The SQL webhook definition.</returns>
    public static WebhookDefinition ToSql(this Concepts.Observation.Webhooks.WebhookDefinition definition) =>
        new()
        {
            Id = definition.Identifier.Value,
            Owner = definition.Owner,
            EventSequenceId = definition.EventSequenceId,
            EventTypes = definition.EventTypes.ToDictionary(
                et => et.ToString(),
                et => "$eventSourceId"),
            Target = definition.Target.ToSql(),
            IsReplayable = definition.IsReplayable,
            IsActive = definition.IsActive
        };

    /// <summary>
    /// Converts a SQL <see cref="WebhookDefinition"/> to a Kernel <see cref="Concepts.Observation.Webhooks.WebhookDefinition"/>.
    /// </summary>
    /// <param name="definition">The SQL webhook definition.</param>
    /// <returns>The Kernel webhook definition.</returns>
    public static Concepts.Observation.Webhooks.WebhookDefinition ToKernel(this WebhookDefinition definition) =>
        new(
            new WebhookId(definition.Id),
            definition.Owner,
            definition.EventSequenceId,
            definition.EventTypes.Select(kvp => EventType.Parse(kvp.Key)),
            definition.Target.ToKernel(),
            definition.IsReplayable);

    static Concepts.Observation.Webhooks.WebhookTarget ToKernel(this WebhookTarget target)
    {
        WebhookAuthorization authorization;

        if (target.BasicAuthUsername is not null && target.BasicAuthPassword is not null)
        {
            authorization = new BasicAuthorization(target.BasicAuthUsername, target.BasicAuthPassword);
        }
        else if (target.BearerToken is not null)
        {
            authorization = new BearerTokenAuthorization(target.BearerToken);
        }
        else if (target.OAuthAuthority is not null && target.OAuthClientId is not null && target.OAuthClientSecret is not null)
        {
            authorization = new OAuthAuthorization(
                target.OAuthAuthority,
                target.OAuthClientId,
                target.OAuthClientSecret);
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

    static WebhookTarget ToSql(this Concepts.Observation.Webhooks.WebhookTarget target)
    {
        var sqlTarget = new WebhookTarget
        {
            Url = target.Url,
            Headers = target.Headers.ToDictionary(kvp => kvp.Key, kvp => kvp.Value)
        };

        target.Authorization.Switch(
            basic =>
            {
                sqlTarget.BasicAuthUsername = basic.Username;
                sqlTarget.BasicAuthPassword = basic.Password;
            },
            bearer => sqlTarget.BearerToken = bearer.Token,
            oauth =>
            {
                sqlTarget.OAuthAuthority = oauth.Authority;
                sqlTarget.OAuthClientId = oauth.ClientId;
                sqlTarget.OAuthClientSecret = oauth.ClientSecret;
            },
            none => { });

        return sqlTarget;
    }
}
