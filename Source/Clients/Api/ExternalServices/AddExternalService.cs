// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Api.ExternalServices;

/// <summary>
/// Represents a command to add an external service.
/// </summary>
public class AddExternalService
{
    /// <summary>
    /// Gets or sets the event store name.
    /// </summary>
    [FromRoute]
    public string EventStore { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the unique identifier of the external service.
    /// </summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the human-readable name of the external service.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the endpoint type.
    /// </summary>
    public ExternalServiceEndpointType EndpointType { get; set; } = ExternalServiceEndpointType.Http;

    /// <summary>
    /// Gets or sets the base URL of the HTTP endpoint.
    /// </summary>
    public string? Url { get; set; }

    /// <summary>
    /// Gets or sets the authorization type for the HTTP endpoint (None, Basic, Bearer, OAuth).
    /// </summary>
    public Security.AuthorizationType AuthorizationType { get; set; } = Security.AuthorizationType.None;

    /// <summary>
    /// Gets or sets the username for Basic authorization.
    /// </summary>
    public string? BasicUsername { get; set; }

    /// <summary>
    /// Gets or sets the password for Basic authorization.
    /// </summary>
    public string? BasicPassword { get; set; }

    /// <summary>
    /// Gets or sets the bearer token for Bearer authorization.
    /// </summary>
    public string? BearerToken { get; set; }

    /// <summary>
    /// Gets or sets the OAuth authority URL.
    /// </summary>
    public string? OAuthAuthority { get; set; }

    /// <summary>
    /// Gets or sets the OAuth client ID.
    /// </summary>
    public string? OAuthClientId { get; set; }

    /// <summary>
    /// Gets or sets the OAuth client secret.
    /// </summary>
    public string? OAuthClientSecret { get; set; }

    /// <summary>
    /// Gets or sets additional headers for the HTTP endpoint.
    /// </summary>
    public IDictionary<string, string> Headers { get; set; } = new Dictionary<string, string>();

    /// <summary>
    /// Gets or sets the database host.
    /// </summary>
    public string? Host { get; set; }

    /// <summary>
    /// Gets or sets the database port. A value of 0 means the provider default is used.
    /// </summary>
    public int Port { get; set; }

    /// <summary>
    /// Gets or sets the database name.
    /// </summary>
    public string? Database { get; set; }

    /// <summary>
    /// Gets or sets the database username.
    /// </summary>
    public string? Username { get; set; }

    /// <summary>
    /// Gets or sets the database password.
    /// </summary>
    public string? Password { get; set; }

    /// <summary>
    /// Gets or sets additional provider-specific database options.
    /// </summary>
    public IDictionary<string, string> Options { get; set; } = new Dictionary<string, string>();
}
