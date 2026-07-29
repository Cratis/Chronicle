// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Contracts.ExternalServices;

/// <summary>
/// Represents the request for getting external services.
/// </summary>
[ProtoContract]
public class GetExternalServicesRequest
{
    /// <summary>
    /// Gets or sets the event store to get external services for.
    /// </summary>
    [ProtoMember(1)]
    public string EventStore { get; set; }
}
