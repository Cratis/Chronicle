// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using ProtoBuf;

namespace Cratis.Chronicle.Contracts;

/// <summary>
/// Represents the request for getting namespaces.
/// </summary>
[ProtoContract]
public record EnsureNamespace
{
    /// <summary>
    /// The event store to ensure namespaces for.
    /// </summary>
    [ProtoMember(1)]
    public string EventStore { get; set; }

    /// <summary>
    /// The name of the namespace.
    /// </summary>
    [ProtoMember(2)]
    public string Name { get; set; } = string.Empty;
}
