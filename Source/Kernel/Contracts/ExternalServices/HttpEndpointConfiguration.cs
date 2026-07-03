// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.Security;

namespace Cratis.Chronicle.Contracts.ExternalServices;

/// <summary>
/// Represents the configuration for an HTTP external service endpoint.
/// </summary>
[ProtoContract]
public class HttpEndpointConfiguration
{
    /// <summary>
    /// Gets or sets the base URL of the endpoint.
    /// </summary>
    [ProtoMember(1)]
    public string Url { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the authorization.
    /// </summary>
    [ProtoMember(2)]
    public OneOf<BasicAuthorization, BearerTokenAuthorization, OAuthAuthorization>? Authorization { get; set; }

    /// <summary>
    /// Gets or sets the headers to send with every request.
    /// </summary>
    [ProtoMember(3, IsRequired = true)]
    public IDictionary<string, string> Headers { get; set; } = new Dictionary<string, string>();
}
