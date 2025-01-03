// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using ProtoBuf;

namespace Cratis.Chronicle.Contracts;

/// <summary>
/// Represents the request for getting namespaces.
/// </summary>
[ProtoContract]
public class GetNamespacesRequest
{
    /// <summary>
    /// Gets or sets the event store to get namespaces for.
    /// </summary>
    [ProtoMember(1)]
    public string EventStore { get; set; }
}
