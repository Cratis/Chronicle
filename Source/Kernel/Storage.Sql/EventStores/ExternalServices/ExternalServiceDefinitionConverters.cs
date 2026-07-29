// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.ExternalServices;
using Cratis.Chronicle.Concepts.Security;

namespace Cratis.Chronicle.Storage.Sql.EventStores.ExternalServices;

/// <summary>
/// Provides extension methods for converting between Kernel and SQL external service representations.
/// </summary>
public static class ExternalServiceDefinitionConverters
{
    /// <summary>
    /// Converts a Kernel <see cref="Concepts.ExternalServices.ExternalServiceDefinition"/> to a SQL <see cref="ExternalServiceDefinition"/>.
    /// </summary>
    /// <param name="definition">The Kernel external service definition.</param>
    /// <returns>The SQL external service definition.</returns>
    public static ExternalServiceDefinition ToSql(this Concepts.ExternalServices.ExternalServiceDefinition definition) =>
        new()
        {
            Id = definition.Id.Value,
            Name = definition.Name,
            Endpoint = definition.Endpoint.ToSql()
        };

    /// <summary>
    /// Converts a SQL <see cref="ExternalServiceDefinition"/> to a Kernel <see cref="Concepts.ExternalServices.ExternalServiceDefinition"/>.
    /// </summary>
    /// <param name="definition">The SQL external service definition.</param>
    /// <returns>The Kernel external service definition.</returns>
    public static Concepts.ExternalServices.ExternalServiceDefinition ToKernel(this ExternalServiceDefinition definition) =>
        new(
            new ExternalServiceId(definition.Id),
            definition.Name,
            definition.Endpoint.ToKernel());

    static ExternalServiceEndpoint ToSql(this Concepts.ExternalServices.ExternalServiceEndpoint endpoint) =>
        new()
        {
            Type = endpoint.Type,
            Http = endpoint.Http?.ToSql(),
            Database = endpoint.Database?.ToSql()
        };

    static Concepts.ExternalServices.ExternalServiceEndpoint ToKernel(this ExternalServiceEndpoint endpoint) =>
        new(
            endpoint.Type,
            endpoint.Http?.ToKernel(),
            endpoint.Database?.ToKernel());

    static HttpEndpointConfiguration ToSql(this Concepts.ExternalServices.HttpEndpointConfiguration configuration)
    {
        var result = new HttpEndpointConfiguration
        {
            Url = configuration.Url,
            Headers = configuration.Headers.ToDictionary(kvp => kvp.Key, kvp => kvp.Value)
        };

        configuration.Authorization.Switch(
            basic =>
            {
                result.BasicAuthUsername = basic.Username;
                result.BasicAuthPassword = basic.Password;
            },
            bearer => result.BearerToken = bearer.Token,
            oauth =>
            {
                result.OAuthAuthority = oauth.Authority;
                result.OAuthClientId = oauth.ClientId;
                result.OAuthClientSecret = oauth.ClientSecret;
            },
            none => { });

        return result;
    }

    static Concepts.ExternalServices.HttpEndpointConfiguration ToKernel(this HttpEndpointConfiguration configuration)
    {
        ExternalServiceAuthorization authorization;

        if (configuration.BasicAuthUsername is not null && configuration.BasicAuthPassword is not null)
        {
            authorization = new BasicAuthorization(configuration.BasicAuthUsername, configuration.BasicAuthPassword);
        }
        else if (configuration.BearerToken is not null)
        {
            authorization = new BearerTokenAuthorization(configuration.BearerToken);
        }
        else if (configuration.OAuthAuthority is not null && configuration.OAuthClientId is not null && configuration.OAuthClientSecret is not null)
        {
            authorization = new OAuthAuthorization(
                configuration.OAuthAuthority,
                configuration.OAuthClientId,
                configuration.OAuthClientSecret);
        }
        else
        {
            authorization = ExternalServiceAuthorization.None;
        }

        return new(configuration.Url, authorization, configuration.Headers.AsReadOnly());
    }

    static DatabaseEndpointConfiguration ToSql(this Concepts.ExternalServices.DatabaseEndpointConfiguration configuration) =>
        new()
        {
            Host = configuration.Host,
            Port = configuration.Port,
            Database = configuration.Database,
            Username = configuration.Username,
            Password = configuration.Password,
            Options = configuration.Options.ToDictionary(kvp => kvp.Key, kvp => kvp.Value)
        };

    static Concepts.ExternalServices.DatabaseEndpointConfiguration ToKernel(this DatabaseEndpointConfiguration configuration) =>
        new(
            configuration.Host,
            configuration.Port,
            configuration.Database,
            configuration.Username,
            configuration.Password,
            configuration.Options.AsReadOnly());
}
