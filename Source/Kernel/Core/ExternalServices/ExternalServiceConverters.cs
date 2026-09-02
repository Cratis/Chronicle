// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.ExternalServices;
using Cratis.Chronicle.Concepts.Security;
using AuthorizationType = Cratis.Chronicle.Contracts.Security.AuthorizationType;

namespace Cratis.Chronicle.ExternalServices;

/// <summary>
/// Provides extension methods for converting between Kernel and contract external service representations.
/// </summary>
public static class ExternalServiceConverters
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

    /// <summary>
    /// Converts kernel external service definitions into the read model the workbench queries.
    /// </summary>
    /// <param name="definitions">The kernel definitions.</param>
    /// <returns>The definitions as read models, without their secrets.</returns>
    internal static IEnumerable<ExternalService> ToReadModel(this IEnumerable<ExternalServiceDefinition> definitions) =>
        [.. definitions.Select(ToReadModel)];

    /// <summary>
    /// Converts a kernel external service definition into the read model the workbench queries.
    /// </summary>
    /// <param name="definition">The kernel definition.</param>
    /// <returns>The definition as a read model, without its secrets.</returns>
    internal static ExternalService ToReadModel(this ExternalServiceDefinition definition) =>
        new(
            definition.Id,
            definition.Name,
            (Contracts.ExternalServices.ExternalServiceEndpointType)definition.Endpoint.Type,
            definition.Endpoint.Http?.Url ?? string.Empty,
            AuthorizationTypeOf(definition.Endpoint.Http),
            definition.Endpoint.Http?.Headers.ToDictionary(_ => _.Key, _ => _.Value) ?? new Dictionary<string, string>(),
            definition.Endpoint.Database?.Host ?? string.Empty,
            definition.Endpoint.Database?.Port ?? 0,
            definition.Endpoint.Database?.Database ?? string.Empty,
            definition.Endpoint.Database?.Username ?? string.Empty,
            definition.Endpoint.Database?.Options.ToDictionary(_ => _.Key, _ => _.Value) ?? new Dictionary<string, string>());

    static AuthorizationType AuthorizationTypeOf(HttpEndpointConfiguration? configuration) =>
        configuration?.Authorization.Match(
            _ => AuthorizationType.Basic,
            _ => AuthorizationType.Bearer,
            _ => AuthorizationType.OAuth,
            _ => AuthorizationType.None) ?? AuthorizationType.None;

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
