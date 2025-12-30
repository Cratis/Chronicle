// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Contracts.ReadModels;

/// <summary>
/// Represents the request for getting read model instances.
/// </summary>
[ProtoContract]
public class GetInstancesRequest
{
    /// <summary>
    /// Gets or sets the name of the event store.
    /// </summary>
    [ProtoMember(1)]
    public string EventStore { get; set; }

    /// <summary>
    /// Gets or sets the namespace of the read model.
    /// </summary>
    [ProtoMember(2)]
    public string Namespace { get; set; }

    /// <summary>
    /// Gets or sets the identifier of the read model.
    /// </summary>
    [ProtoMember(3)]
    public string ReadModel { get; set; }

    /// <summary>
    /// Gets or sets the optional occurrence name to get instances from. If not provided, gets from the default/current model.
    /// </summary>
    [ProtoMember(4)]
    public string? Occurrence { get; set; }

    /// <summary>
    /// Gets or sets the page number for pagination.
    /// </summary>
    [ProtoMember(5)]
    public int Page { get; set; }

    /// <summary>
    /// Gets or sets the page size for pagination.
    /// </summary>
    [ProtoMember(6)]
    public int PageSize { get; set; }
}
