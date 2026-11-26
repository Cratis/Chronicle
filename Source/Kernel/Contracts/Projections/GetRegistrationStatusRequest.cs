// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Contracts.Projections;

/// <summary>
/// Represents the request for the state of the projections a registrant has registered.
/// </summary>
[ProtoContract]
public class GetRegistrationStatusRequest
{
    /// <summary>
    /// Gets or sets the event store name.
    /// </summary>
    [ProtoMember(1)]
    public string EventStore { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the registrant, as it identified itself when it registered.
    /// </summary>
    [ProtoMember(2)]
    public string Registrant { get; set; } = string.Empty;
}
