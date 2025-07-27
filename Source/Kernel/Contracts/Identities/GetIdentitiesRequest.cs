// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Contracts.Identities;

/// <summary>
///  Represents the request for getting identities.
/// </summary>
[ProtoContract]
public class GetIdentitiesRequest
{
    /// <summary>
    /// The event store to get identities for.
    /// </summary>
    [ProtoMember(1)]
    public string EventStore { get; set; }

    /// <summary>
    /// The namespace to get identities for.
    /// </summary>
    [ProtoMember(2)]
    public string Namespace { get; set; }
}
