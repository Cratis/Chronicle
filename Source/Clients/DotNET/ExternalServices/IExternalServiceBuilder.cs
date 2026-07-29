// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.ExternalServices;

/// <summary>
/// Defines a fluent builder for configuring an external service.
/// </summary>
public interface IExternalServiceBuilder
{
    /// <summary>
    /// Configures the service as an HTTP endpoint.
    /// </summary>
    /// <param name="url">The base URL of the endpoint.</param>
    /// <returns>The builder continuation.</returns>
    IExternalServiceBuilder Http(string url);

    /// <summary>
    /// Configures basic authentication for an HTTP endpoint.
    /// </summary>
    /// <param name="username">The username.</param>
    /// <param name="password">The password.</param>
    /// <returns>The builder continuation.</returns>
    IExternalServiceBuilder WithBasicAuth(string username, string password);

    /// <summary>
    /// Configures bearer token authentication for an HTTP endpoint.
    /// </summary>
    /// <param name="token">The bearer token.</param>
    /// <returns>The builder continuation.</returns>
    IExternalServiceBuilder WithBearerToken(string token);

    /// <summary>
    /// Configures OAuth authentication for an HTTP endpoint.
    /// </summary>
    /// <param name="authority">The OAuth authority.</param>
    /// <param name="clientId">The OAuth client ID.</param>
    /// <param name="clientSecret">The OAuth client secret.</param>
    /// <returns>The builder continuation.</returns>
    IExternalServiceBuilder WithOAuth(string authority, string clientId, string clientSecret);

    /// <summary>
    /// Adds a header to send with every HTTP request.
    /// </summary>
    /// <param name="key">The header key.</param>
    /// <param name="value">The header value.</param>
    /// <returns>The builder continuation.</returns>
    IExternalServiceBuilder WithHeader(string key, string value);

    /// <summary>
    /// Configures the service as a Microsoft SQL Server database endpoint.
    /// </summary>
    /// <param name="host">The database host.</param>
    /// <param name="database">The database name.</param>
    /// <param name="username">The username used to connect.</param>
    /// <param name="password">The password used to connect.</param>
    /// <param name="port">The database port. Leave as 0 to use the provider default.</param>
    /// <returns>The builder continuation.</returns>
    IExternalServiceBuilder MsSql(string host, string database, string username, string password, int port = 0);

    /// <summary>
    /// Configures the service as a PostgreSQL database endpoint.
    /// </summary>
    /// <param name="host">The database host.</param>
    /// <param name="database">The database name.</param>
    /// <param name="username">The username used to connect.</param>
    /// <param name="password">The password used to connect.</param>
    /// <param name="port">The database port. Leave as 0 to use the provider default.</param>
    /// <returns>The builder continuation.</returns>
    IExternalServiceBuilder PostgreSql(string host, string database, string username, string password, int port = 0);

    /// <summary>
    /// Adds a provider-specific option to a database endpoint's connection string.
    /// </summary>
    /// <param name="key">The option key.</param>
    /// <param name="value">The option value.</param>
    /// <returns>The builder continuation.</returns>
    IExternalServiceBuilder WithOption(string key, string value);
}
