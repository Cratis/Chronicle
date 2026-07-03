// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Contracts.ExternalServices;

/// <summary>
/// Represents the endpoint an external service exposes.
/// </summary>
[ProtoContract]
public class ExternalServiceEndpoint
{
    /// <summary>
    /// Gets or sets the <see cref="ExternalServiceEndpointType"/>.
    /// </summary>
    [ProtoMember(1)]
    public ExternalServiceEndpointType Type { get; set; }

    /// <summary>
    /// Gets or sets the HTTP configuration, set when <see cref="Type"/> is <see cref="ExternalServiceEndpointType.Http"/>.
    /// </summary>
    [ProtoMember(2)]
    public HttpEndpointConfiguration? Http { get; set; }

    /// <summary>
    /// Gets or sets the database configuration, set for database endpoint types.
    /// </summary>
    [ProtoMember(3)]
    public DatabaseEndpointConfiguration? Database { get; set; }
}
