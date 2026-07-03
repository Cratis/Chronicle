// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.ExternalServices;
using Cratis.Chronicle.Concepts.Security;

namespace Cratis.Chronicle.Storage.MongoDB.ExternalServices;

/// <summary>
/// Provides extension methods for converting between Kernel and MongoDB external service representations.
/// </summary>
public static class ExternalServiceDefinitionConverters
{
    /// <summary>
    /// Converts a Kernel <see cref="Concepts.ExternalServices.ExternalServiceDefinition"/> to a MongoDB <see cref="ExternalServiceDefinition"/>.
    /// </summary>
    /// <param name="definition">The Kernel external service definition.</param>
    /// <returns>The MongoDB external service definition.</returns>
    public static ExternalServiceDefinition ToMongoDB(this Concepts.ExternalServices.ExternalServiceDefinition definition) =>
        new()
        {
            Id = definition.Id,
            Name = definition.Name,
            Endpoint = definition.Endpoint.ToMongoDB()
        };

    /// <summary>
    /// Converts a MongoDB <see cref="ExternalServiceDefinition"/> to a Kernel <see cref="Concepts.ExternalServices.ExternalServiceDefinition"/>.
    /// </summary>
    /// <param name="definition">The MongoDB external service definition.</param>
    /// <returns>The Kernel external service definition.</returns>
    public static Concepts.ExternalServices.ExternalServiceDefinition ToKernel(this ExternalServiceDefinition definition) =>
        new(
            definition.Id,
            definition.Name,
            definition.Endpoint.ToKernel());

    static ExternalServiceEndpoint ToMongoDB(this Concepts.ExternalServices.ExternalServiceEndpoint endpoint) =>
        new()
        {
            Type = endpoint.Type,
            Http = endpoint.Http?.ToMongoDB(),
            Database = endpoint.Database?.ToMongoDB()
        };

    static Concepts.ExternalServices.ExternalServiceEndpoint ToKernel(this ExternalServiceEndpoint endpoint) =>
        new(
            endpoint.Type,
            endpoint.Http?.ToKernel(),
            endpoint.Database?.ToKernel());

    static HttpEndpointConfiguration ToMongoDB(this Concepts.ExternalServices.HttpEndpointConfiguration configuration)
    {
        var result = new HttpEndpointConfiguration
        {
            Url = configuration.Url,
            Headers = configuration.Headers.ToDictionary(kvp => kvp.Key, kvp => kvp.Value)
        };

        configuration.Authorization.Switch(
            basic => result.BasicAuthorization = new Security.BasicAuthorization
            {
                Username = basic.Username,
                Password = basic.Password
            },
            bearer => result.BearerTokenAuthorization = new Security.BearerTokenAuthorization
            {
                Token = bearer.Token
            },
            oauth => result.OAuthAuthorization = new Security.OAuthAuthorization
            {
                Authority = oauth.Authority,
                ClientId = oauth.ClientId,
                ClientSecret = oauth.ClientSecret
            },
            none => { });

        return result;
    }

    static Concepts.ExternalServices.HttpEndpointConfiguration ToKernel(this HttpEndpointConfiguration configuration)
    {
        ExternalServiceAuthorization authorization;

        if (configuration.BasicAuthorization is not null)
        {
            authorization = new BasicAuthorization(configuration.BasicAuthorization.Username, configuration.BasicAuthorization.Password);
        }
        else if (configuration.BearerTokenAuthorization is not null)
        {
            authorization = new BearerTokenAuthorization(configuration.BearerTokenAuthorization.Token);
        }
        else if (configuration.OAuthAuthorization is not null)
        {
            authorization = new OAuthAuthorization(
                configuration.OAuthAuthorization.Authority,
                configuration.OAuthAuthorization.ClientId,
                configuration.OAuthAuthorization.ClientSecret);
        }
        else
        {
            authorization = ExternalServiceAuthorization.None;
        }

        return new(configuration.Url, authorization, configuration.Headers.AsReadOnly());
    }

    static DatabaseEndpointConfiguration ToMongoDB(this Concepts.ExternalServices.DatabaseEndpointConfiguration configuration) =>
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
