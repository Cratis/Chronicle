// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Contracts.ReadModels;

/// <summary>
/// Represents the request for observing instances of a read model.
/// </summary>
[ProtoContract]
public class ObserveInstancesRequest
{
    /// <summary>
    /// Gets or sets the event store name.
    /// </summary>
    [ProtoMember(1)]
    public string EventStore { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the namespace.
    /// </summary>
    [ProtoMember(2)]
    public string Namespace { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the read model identifier.
    /// </summary>
    [ProtoMember(3)]
    public string ReadModel { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the page number.
    /// </summary>
    [ProtoMember(4)]
    public int Page { get; set; }

    /// <summary>
    /// Gets or sets the page size.
    /// </summary>
    [ProtoMember(5)]
    public int PageSize { get; set; }

    /// <summary>
    /// Gets or sets the optional occurrence to get instances from.
    /// </summary>
    [ProtoMember(6)]
    public string? Occurrence { get; set; }
}
