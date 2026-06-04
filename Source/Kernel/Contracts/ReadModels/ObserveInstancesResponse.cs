// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Contracts.ReadModels;

/// <summary>
/// Represents the response for observing instances of a read model.
/// </summary>
[Message]
public class ObserveInstancesResponse
{
    /// <summary>
    /// Gets or sets the list of instances as JSON strings.
    /// </summary>
    public List<string> Instances { get; set; } = [];

    /// <summary>
    /// Gets or sets the total count of instances available.
    /// </summary>
    public int TotalCount { get; set; }

    /// <summary>
    /// Gets or sets the page number.
    /// </summary>
    public int Page { get; set; }

    /// <summary>
    /// Gets or sets the page size.
    /// </summary>
    public int PageSize { get; set; }
}
