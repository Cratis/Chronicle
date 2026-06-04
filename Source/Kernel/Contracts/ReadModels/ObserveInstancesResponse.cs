// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Contracts.ReadModels;

/// <summary>
/// Represents the response for observing instances of a read model.
/// </summary>
[ProtoContract]
public class ObserveInstancesResponse
{
    /// <summary>
    /// Gets or sets the list of instances as JSON strings.
    /// </summary>
    [ProtoMember(1)]
    public IList<string> Instances { get; set; } = [];

    /// <summary>
    /// Gets or sets the total count of instances available.
    /// </summary>
    [ProtoMember(2)]
    public int TotalCount { get; set; }

    /// <summary>
    /// Gets or sets the page number.
    /// </summary>
    [ProtoMember(3)]
    public int Page { get; set; }

    /// <summary>
    /// Gets or sets the page size.
    /// </summary>
    [ProtoMember(4)]
    public int PageSize { get; set; }
}
