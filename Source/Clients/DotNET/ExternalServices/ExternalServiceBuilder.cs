// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.ExternalServices;
using Cratis.Chronicle.Contracts.Primitives;
using Cratis.Chronicle.Contracts.Security;

namespace Cratis.Chronicle.ExternalServices;

/// <summary>
/// Represents an implementation of <see cref="IExternalServiceBuilder"/>.
/// </summary>
public class ExternalServiceBuilder : IExternalServiceBuilder
{
    readonly Dictionary<string, string> _headers = [];
    readonly Dictionary<string, string> _options = [];
    ExternalServiceEndpointType _type = ExternalServiceEndpointType.Http;
    string _url = string.Empty;
    OneOf<BasicAuthorization, BearerTokenAuthorization, OAuthAuthorization>? _authorization;
    string _host = string.Empty;
    int _port;
    string _database = string.Empty;
    string _username = string.Empty;
    string _password = string.Empty;

    /// <inheritdoc/>
    public IExternalServiceBuilder Http(string url)
    {
        _type = ExternalServiceEndpointType.Http;
        _url = url;

        return this;
    }

    /// <inheritdoc/>
    public IExternalServiceBuilder WithBasicAuth(string username, string password)
    {
        _authorization = new(new BasicAuthorization { Username = username, Password = password });

        return this;
    }

    /// <inheritdoc/>
    public IExternalServiceBuilder WithBearerToken(string token)
    {
        _authorization = new(new BearerTokenAuthorization { Token = token });

        return this;
    }

    /// <inheritdoc/>
    public IExternalServiceBuilder WithOAuth(string authority, string clientId, string clientSecret)
    {
        _authorization = new(new OAuthAuthorization { Authority = authority, ClientId = clientId, ClientSecret = clientSecret });

        return this;
    }

    /// <inheritdoc/>
    public IExternalServiceBuilder WithHeader(string key, string value)
    {
        _headers[key] = value;

        return this;
    }

    /// <inheritdoc/>
    public IExternalServiceBuilder MsSql(string host, string database, string username, string password, int port = 0) =>
        ConfigureDatabase(ExternalServiceEndpointType.MsSql, host, database, username, password, port);

    /// <inheritdoc/>
    public IExternalServiceBuilder PostgreSql(string host, string database, string username, string password, int port = 0) =>
        ConfigureDatabase(ExternalServiceEndpointType.PostgreSql, host, database, username, password, port);

    /// <inheritdoc/>
    public IExternalServiceBuilder WithOption(string key, string value)
    {
        _options[key] = value;

        return this;
    }

    /// <summary>
    /// Builds the <see cref="ExternalServiceDefinition"/>.
    /// </summary>
    /// <param name="id">The identifier of the external service.</param>
    /// <param name="name">The human-readable name of the external service.</param>
    /// <returns>The built <see cref="ExternalServiceDefinition"/>.</returns>
    public ExternalServiceDefinition Build(string id, string name)
    {
        var endpoint = new ExternalServiceEndpoint { Type = _type };

        if (_type == ExternalServiceEndpointType.Http)
        {
            endpoint.Http = new HttpEndpointConfiguration
            {
                Url = _url,
                Authorization = _authorization,
                Headers = _headers
            };
        }
        else
        {
            endpoint.Database = new DatabaseEndpointConfiguration
            {
                Host = _host,
                Port = _port,
                Database = _database,
                Username = _username,
                Password = _password,
                Options = _options
            };
        }

        return new ExternalServiceDefinition
        {
            Id = id,
            Name = name,
            Endpoint = endpoint
        };
    }

    ExternalServiceBuilder ConfigureDatabase(ExternalServiceEndpointType type, string host, string database, string username, string password, int port)
    {
        _type = type;
        _host = host;
        _database = database;
        _username = username;
        _password = password;
        _port = port;

        return this;
    }
}
