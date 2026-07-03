// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Api.ExternalServices;

/// <summary>
/// Represents an external service definition as exposed to the workbench.
/// </summary>
/// <remarks>
/// Secrets (passwords, tokens, client secrets) are intentionally not exposed through this read model.
/// </remarks>
public class ExternalServiceDefinition
{
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
    /// Gets or sets the authorization type for the HTTP endpoint.
    /// </summary>
    public Security.AuthorizationType AuthorizationType { get; set; } = Security.AuthorizationType.None;

    /// <summary>
    /// Gets or sets additional headers for the HTTP endpoint.
    /// </summary>
    public IDictionary<string, string> Headers { get; set; } = new Dictionary<string, string>();

    /// <summary>
    /// Gets or sets the database host.
    /// </summary>
    public string? Host { get; set; }

    /// <summary>
    /// Gets or sets the database port.
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
    /// Gets or sets additional provider-specific database options.
    /// </summary>
    public IDictionary<string, string> Options { get; set; } = new Dictionary<string, string>();
}
