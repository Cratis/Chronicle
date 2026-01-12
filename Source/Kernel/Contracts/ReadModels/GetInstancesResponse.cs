// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Contracts.ReadModels;

/// <summary>
/// Represents the response for getting read model instances.
/// </summary>
[ProtoContract]
public class GetInstancesResponse
{
    /// <summary>
    /// Gets or sets the collection of instances as JSON strings.
    /// </summary>
    [ProtoMember(1)]
    public IEnumerable<string> Instances { get; set; }

    /// <summary>
    /// Gets or sets the total count of instances.
    /// </summary>
    [ProtoMember(2)]
    public long TotalCount { get; set; }

    /// <summary>
    /// Gets or sets the current page number.
    /// </summary>
    [ProtoMember(3)]
    public int Page { get; set; }

    /// <summary>
    /// Gets or sets the page size used.
    /// </summary>
    [ProtoMember(4)]
    public int PageSize { get; set; }
}
