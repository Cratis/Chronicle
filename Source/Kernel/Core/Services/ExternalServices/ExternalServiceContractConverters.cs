// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.ExternalServices;
using Cratis.Chronicle.Concepts.Security;

namespace Cratis.Chronicle.Services.ExternalServices;

/// <summary>
/// Provides extension methods for converting between Kernel and contract external service representations.
/// </summary>
public static class ExternalServiceContractConverters
{
    /// <summary>
    /// Converts a contract external service definition to a Kernel <see cref="ExternalServiceDefinition"/>.
    /// </summary>
    /// <param name="definition">The contract external service definition.</param>
    /// <returns>The Kernel external service definition.</returns>
    public static ExternalServiceDefinition ToKernel(this Contracts.ExternalServices.ExternalServiceDefinition definition) =>
        new(
            new ExternalServiceId(definition.Id),
            new ExternalServiceName(definition.Name),
            definition.Endpoint.ToKernel());

    /// <summary>
    /// Converts a Kernel <see cref="ExternalServiceDefinition"/> to a contract external service definition.
    /// </summary>
    /// <param name="definition">The Kernel external service definition.</param>
    /// <returns>The contract external service definition.</returns>
    public static Contracts.ExternalServices.ExternalServiceDefinition ToContract(this ExternalServiceDefinition definition) =>
        new()
        {
            Id = definition.Id,
            Name = definition.Name,
            Endpoint = definition.Endpoint.ToContract()
        };

    static ExternalServiceEndpoint ToKernel(this Contracts.ExternalServices.ExternalServiceEndpoint endpoint) =>
        new(
            (ExternalServiceEndpointType)endpoint.Type,
            endpoint.Http?.ToKernel(),
            endpoint.Database?.ToKernel());

    static Contracts.ExternalServices.ExternalServiceEndpoint ToContract(this ExternalServiceEndpoint endpoint) =>
        new()
        {
            Type = (Contracts.ExternalServices.ExternalServiceEndpointType)endpoint.Type,
            Http = endpoint.Http?.ToContract(),
            Database = endpoint.Database?.ToContract()
        };

    static HttpEndpointConfiguration ToKernel(this Contracts.ExternalServices.HttpEndpointConfiguration configuration)
    {
        var authorization = ExternalServiceAuthorization.None;

        if (configuration.Authorization is { } auth)
        {
            if (auth.Value0 is { } basic)
            {
                authorization = new BasicAuthorization(basic.Username, basic.Password);
            }
            else if (auth.Value1 is { } bearer)
            {
                authorization = new BearerTokenAuthorization(bearer.Token);
            }
            else if (auth.Value2 is { } oauth)
            {
                authorization = new OAuthAuthorization(oauth.Authority, oauth.ClientId, oauth.ClientSecret);
            }
        }

        return new(configuration.Url, authorization, configuration.Headers.AsReadOnly());
    }

    static Contracts.ExternalServices.HttpEndpointConfiguration ToContract(this HttpEndpointConfiguration configuration)
    {
        var result = new Contracts.ExternalServices.HttpEndpointConfiguration
        {
            Url = configuration.Url,
            Headers = configuration.Headers.ToDictionary(kvp => kvp.Key, kvp => kvp.Value)
        };

        configuration.Authorization.Switch(
            basic => result.Authorization = new(new Contracts.Security.BasicAuthorization
            {
                Username = basic.Username,
                Password = basic.Password
            }),
            bearer => result.Authorization = new(new Contracts.Security.BearerTokenAuthorization
            {
                Token = bearer.Token
            }),
            oauth => result.Authorization = new(new Contracts.Security.OAuthAuthorization
            {
                Authority = oauth.Authority,
                ClientId = oauth.ClientId,
                ClientSecret = oauth.ClientSecret
            }),
            none => { });

        return result;
    }

    static DatabaseEndpointConfiguration ToKernel(this Contracts.ExternalServices.DatabaseEndpointConfiguration configuration) =>
        new(
            configuration.Host,
            configuration.Port,
            configuration.Database,
            configuration.Username,
            configuration.Password,
            configuration.Options.AsReadOnly());

    static Contracts.ExternalServices.DatabaseEndpointConfiguration ToContract(this DatabaseEndpointConfiguration configuration) =>
        new()
        {
            Host = configuration.Host,
            Port = configuration.Port,
            Database = configuration.Database,
            Username = configuration.Username,
            Password = configuration.Password,
            Options = configuration.Options.ToDictionary(kvp => kvp.Key, kvp => kvp.Value)
        };
}
